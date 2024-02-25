using System.Linq;
using System.Security.Cryptography;
using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;
using TonLibDotNet.Types;

namespace SomeDAO.Backend.Services
{
    public class SyncTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        private const int MaxBatch = 100;

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly DataParser dataParser;
        private readonly SyncSchedulerService syncScheduler;
        private readonly CachedData cachedData;
        private readonly ITask cachedDataTask;
        private readonly ITask translateTask;

        public SyncTask(ILogger<SyncTask> logger, IDbProvider dbProvider, DataParser dataParser, SyncSchedulerService syncScheduler, CachedData cachedData, ITask<CachedData> cachedDataTask, ITask<TranslateTask> translateTask)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
            this.syncScheduler = syncScheduler ?? throw new ArgumentNullException(nameof(syncScheduler));
            this.cachedData = cachedData ?? throw new ArgumentNullException(nameof(cachedData));
            this.cachedDataTask = cachedDataTask;
            this.translateTask = translateTask;
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            // Init TonClient and retry if needed, before actually syncing entities.
            currentTask.Options.Interval = GetTaskDelay(currentTask.RunStatus.FailsCount);
            await dataParser.EnsureSynced().ConfigureAwait(false);

            var db = dbProvider.MainDb;
            var counter = 0;
            while (counter < MaxBatch)
            {
                var next = await db.Table<SyncQueueItem>().OrderBy(x => x.SyncAt).FirstOrDefaultAsync();

                if (next == null)
                {
                    logger.LogDebug("No [more] data to sync.");
                    currentTask.Options.Interval = Interval;
                    break;
                }

                var wait = next.SyncAt - DateTimeOffset.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    logger.LogDebug("Next ({Type} #{Index}) sync in {Wait} at {Time}, will wait.", next.EntityType, next.Index, wait, next.SyncAt);
                    currentTask.Options.Interval = wait < Interval ? wait : Interval;
                    break;
                }

                counter++;

                try
                {
                    logger.LogDebug("Sync #{Counter} ({Type} #{Index}) started...", counter, next.EntityType, next.Index);
                    var task = next.EntityType switch
                    {
                        EntityType.Admin => SyncAdmin(next.Index),
                        EntityType.User => SyncUser(next.Index),
                        EntityType.Order => SyncOrder(next.Index),
                        EntityType.Master => SyncMaster(),
                        _ => Task.FromResult(DateTimeOffset.MaxValue),
                    };

                    var lastSync = await task.ConfigureAwait(false);

                    var deleted = await db.Table<SyncQueueItem>().Where(x => x.Index == next.Index && x.EntityType == next.EntityType && x.MinLastSync <= lastSync).DeleteAsync();

                    if (lastSync == DateTimeOffset.MaxValue)
                    {
                        logger.LogWarning("Sync #{Counter} ({Type} #{Index}) SKIPPED, deleted {Count} sync item(s) from queue.", counter, next.EntityType, next.Index, deleted);
                    }
                    else
                    {
                        logger.LogDebug("Sync #{Counter} ({Type} #{Index}) done, last_sync={LastSync}, deleted {Count} sync item(s) from queue.", counter, next.EntityType, next.Index, lastSync, deleted);
                        if (lastSync < next.MinLastSync)
                        {
                            var delay = GetDelay(next.RetryCount);
                            logger.LogWarning("Sync #{Counter} ({Type} #{Index}) sync less than required ({MinSync}), will retry in {Delay}.", counter, next.EntityType, next.Index, next.MinLastSync, delay);
                            next.SyncAt = DateTimeOffset.UtcNow + delay;
                            next.RetryCount += 1;
                            await db.InsertOrReplaceAsync(next).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var delay = GetDelay(next.RetryCount);
                    logger.LogError(ex, "Sync #{Counter} ({Type} #{Index}) failed, will retry in {Delay}.", counter, next.EntityType, next.Index, delay);
                    next.SyncAt = DateTimeOffset.UtcNow + delay;
                    next.RetryCount += 1;
                    await db.UpdateAsync(next).ConfigureAwait(false);
                }
            }

            if (counter > 0)
            {
                cachedDataTask.TryRunImmediately();
                translateTask.TryRunImmediately();
            }
        }

        protected async Task<DateTimeOffset> SyncAdmin(long index)
        {
            var admin = await dbProvider.MainDb.Table<Admin>().FirstOrDefaultAsync(x => x.Index == index);
            if (admin == null)
            {
                logger.LogWarning("Admin #{Index} was not found, nothing to sync", index);
                return DateTimeOffset.MaxValue;
            }

            await dataParser.UpdateAdmin(admin).ConfigureAwait(false);
            await dbProvider.MainDb.InsertOrReplaceAsync(admin).ConfigureAwait(false);
            return admin.LastSync;
        }

        protected async Task<DateTimeOffset> SyncUser(long index)
        {
            var user = await dbProvider.MainDb.Table<User>().FirstOrDefaultAsync(x => x.Index == index);
            if (user == null)
            {
                logger.LogWarning("User #{Index} was not found, nothing to sync", index);
                return DateTimeOffset.MaxValue;
            }

            var changed = await dataParser.UpdateUser(user).ConfigureAwait(false);

            if (changed)
            {
                user.AboutHash = string.IsNullOrWhiteSpace(user.About) ? null : SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(user.About));
                user.NeedTranslation = true;
            }

            await dbProvider.MainDb.InsertOrReplaceAsync(user).ConfigureAwait(false);
            return user.LastSync;
        }

        protected async Task<DateTimeOffset> SyncOrder(long index)
        {
            var order = await dbProvider.MainDb.Table<Order>().FirstOrDefaultAsync(x => x.Index == index);
            if (order == null)
            {
                logger.LogWarning("Order #{Index} was not found, nothing to sync", index);
                return DateTimeOffset.MaxValue;
            }

            var endLt = order.LastTxLt;
            var changed = await dataParser.UpdateOrder(order).ConfigureAwait(false);

            if (changed)
            {
                order.NameHash = string.IsNullOrWhiteSpace(order.Name) ? null : SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(order.Name));
                order.DescriptionHash = string.IsNullOrWhiteSpace(order.Description) ? null : SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(order.Description));
                order.TechnicalTaskHash = string.IsNullOrWhiteSpace(order.TechnicalTask) ? null : SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(order.TechnicalTask));
                order.NeedTranslation = true;
            }

            await foreach (var activity in dataParser.GetOrderActivities(order, endLt))
            {
                if (activity.OpCode == OpCode.InitOrder)
                {
                    order.CreatedAt = activity.Timestamp;
                }

                var exist = await dbProvider.MainDb.Table<OrderActivity>().CountAsync(x => x.OrderId == order.Id && x.TxLt == activity.TxLt);
                if (exist == 0)
                {
                    logger.LogDebug("Tx for Order {Address} added: Op {OpCode} at {Time} ({Lt}/{Hash})", order.Address, activity.OpCode, activity.Timestamp, activity.TxLt, activity.TxHash);
                    await dbProvider.MainDb.InsertAsync(activity).ConfigureAwait(false);
                }
                else
                {
                    logger.LogDebug("Tx for Order {Address} already exists: Op {OpCode} at {Time} ({Lt}/{Hash})", order.Address, activity.OpCode, activity.Timestamp, activity.TxLt, activity.TxHash);
                }
            }

            await dbProvider.MainDb.InsertOrReplaceAsync(order).ConfigureAwait(false);

            return order.LastSync;
        }

        protected async Task<DateTimeOffset> SyncMaster()
        {
            var db = dbProvider.MainDb;
            var master = await db.FindAsync<Settings>(Settings.MASTER_ADDRESS).ConfigureAwait(false);
            var masterAddress = master.StringValue!;

            var md = await dataParser.ParseMasterData(masterAddress);

            // Create missing Admins
            var nextAdmin = (await db.FindAsync<Settings>(Settings.NEXT_INDEX_ADMIN))?.LongValue ?? 0;
            if (nextAdmin < md.nextAdminIndex)
            {
                await foreach (var (index, address) in dataParser.EnumerateAdminAddresses(masterAddress, nextAdmin, md.nextAdminIndex))
                {
                    var entity = await db.FindAsync<Admin>(x => x.Index == index).ConfigureAwait(false);
                    if (entity == null)
                    {
                        entity = new Admin()
                        {
                            Index = index,
                            Address = TonUtils.Address.SetBounceable(address, true),
                            AdminAddress = masterAddress,
                        };
                        await db.InsertAsync(entity).ConfigureAwait(false);
                        await syncScheduler.Schedule(entity);
                        logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                    }
                }

                await db.InsertOrReplaceAsync(new Settings(Settings.NEXT_INDEX_ADMIN, md.nextAdminIndex)).ConfigureAwait(false);
            }

            // Create missing Users
            var nextUser = (await db.FindAsync<Settings>(Settings.NEXT_INDEX_USER))?.LongValue ?? 0;
            if (nextUser < md.nextUserIndex)
            {
                await foreach (var (index, address) in dataParser.EnumerateUserAddresses(masterAddress, nextUser, md.nextUserIndex))
                {
                    var entity = await db.FindAsync<User>(x => x.Index == index).ConfigureAwait(false);
                    if (entity == null)
                    {
                        entity = new User()
                        {
                            Index = index,
                            Address = TonUtils.Address.SetBounceable(address, true),
                            UserAddress = masterAddress,
                        };
                        await db.InsertAsync(entity).ConfigureAwait(false);
                        await syncScheduler.Schedule(entity);
                        logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                    }
                }

                await db.InsertOrReplaceAsync(new Settings(Settings.NEXT_INDEX_USER, md.nextUserIndex)).ConfigureAwait(false);
            }

            // Create missing Orders
            var nextOrder = (await db.FindAsync<Settings>(Settings.NEXT_INDEX_ORDER))?.LongValue ?? 0;
            if (nextOrder < md.nextOrderIndex)
            {
                await foreach (var (index, address) in dataParser.EnumerateOrderAddresses(masterAddress, nextOrder, md.nextOrderIndex))
                {
                    var entity = await db.FindAsync<Order>(x => x.Index == index).ConfigureAwait(false);
                    if (entity == null)
                    {
                        entity = new Order()
                        {
                            Index = index,
                            Address = TonUtils.Address.SetBounceable(address, true),
                            CustomerAddress = masterAddress,
                        };
                        await db.InsertAsync(entity).ConfigureAwait(false);
                        await syncScheduler.Schedule(entity);
                        logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                    }
                }

                await db.InsertOrReplaceAsync(new Settings(Settings.NEXT_INDEX_ORDER, md.nextOrderIndex)).ConfigureAwait(false);
            }

            // Recreate all categories
            await db.Table<Category>().DeleteAsync(x => true).ConfigureAwait(false);
            var catCount = await db.InsertAllAsync(md.categories).ConfigureAwait(false);
            logger.LogDebug("Reloaded {Count} categories", catCount);

            // Recreate all languages
            var langOldCount = await db.Table<Language>().DeleteAsync(x => true).ConfigureAwait(false);
            var langNewCount = await db.InsertAllAsync(md.languages).ConfigureAwait(false);
            logger.LogDebug("Reloaded {Count} languages", langNewCount);
            if (langOldCount != langNewCount)
            {
                var c1 = await db.ExecuteAsync($"UPDATE [{nameof(Order)}] SET {nameof(Order.NeedTranslation)} = 1");
                var c2 = await db.ExecuteAsync($"UPDATE [{nameof(User)}] SET {nameof(User.NeedTranslation)} = 1");
                logger.LogDebug("A number of languages changed, so {Count} orders and {Count} users were marked for re-translation", c1, c2);
            }

            // Check TX history and re-sync contracts
            var endLt = await dbProvider.MainDb.FindAsync<Settings>(Settings.LAST_MASTER_TX_LT).ConfigureAwait(false);
            if (endLt == null || md.lastTx.Lt != endLt.LongValue)
            {
                var found = 0;
                await foreach (var tx in dataParser.EnumerateTransactions(masterAddress, md.lastTx, endLt?.LongValue ?? 0))
                {
                    IEnumerable<AccountAddress> arr = new[] { tx.InMsg!.Source };
                    if (tx.OutMsgs != null)
                    {
                        arr = arr.Concat(tx.OutMsgs.Select(x => x.Destination));
                    }

                    foreach (var item in arr)
                    {
                        var adr = TonUtils.Address.SetBounceable(item.Value, true);

                        var a = cachedData.AllAdmins.Find(x => StringComparer.Ordinal.Equals(x.Address, adr));
                        if (a != null)
                        {
                            await syncScheduler.Schedule(a).ConfigureAwait(false);
                            found++;
                        }

                        var u = cachedData.AllUsers.Find(x => StringComparer.Ordinal.Equals(x.Address, adr));
                        if (u != null)
                        {
                            await syncScheduler.Schedule(u).ConfigureAwait(false);
                            found++;
                        }

                        var o = cachedData.AllOrders.Find(x => StringComparer.Ordinal.Equals(x.Address, adr));
                        if (o != null)
                        {
                            await syncScheduler.Schedule(o).ConfigureAwait(false);
                            found++;
                        }
                    }
                }

                logger.LogDebug("Checked transactions to Lt={Lt}, found {Count} known addresses (non unique!) for sync.", md.lastTx.Lt, found);
            }

            await db.InsertOrReplaceAsync(new Settings(Settings.LAST_MASTER_DATA_HASH, md.stateHash)).ConfigureAwait(false);
            await db.InsertOrReplaceAsync(new Settings(Settings.LAST_MASTER_TX_LT, md.lastTx.Lt)).ConfigureAwait(false);

            return md.syncTime;
        }

        private static TimeSpan GetTaskDelay(int failsCount)
        {
            return failsCount switch
            {
                0 => TimeSpan.FromSeconds(5),
                1 => TimeSpan.FromSeconds(5),
                2 => TimeSpan.FromSeconds(5),
                3 => TimeSpan.FromSeconds(10),
                4 => TimeSpan.FromSeconds(15),
                5 => TimeSpan.FromSeconds(30),
                6 => TimeSpan.FromSeconds(60),
                7 => TimeSpan.FromMinutes(2),
                8 => TimeSpan.FromMinutes(5),
                9 => TimeSpan.FromMinutes(10),
                _ => TimeSpan.FromMinutes(30),
            };
        }

        private static TimeSpan GetDelay(int retryCount)
        {
            // Actually, high delays are useless,
            //   because ForceRecynsTask re-schedule objects more frequently (at *ForceResyncInterval).
            return retryCount switch
            {
                0 => TimeSpan.FromSeconds(5),
                1 => TimeSpan.FromSeconds(5),
                2 => TimeSpan.FromSeconds(5),
                3 => TimeSpan.FromSeconds(10),
                4 => TimeSpan.FromSeconds(15),
                5 => TimeSpan.FromSeconds(30),
                6 => TimeSpan.FromSeconds(60),
                7 => TimeSpan.FromMinutes(2),
                8 => TimeSpan.FromMinutes(5),
                9 => TimeSpan.FromMinutes(10),
                10 => TimeSpan.FromMinutes(30),
                11 => TimeSpan.FromHours(1),
                12 => TimeSpan.FromHours(4),
                _ => TimeSpan.FromHours(13),
            };
        }
    }
}
