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
        private readonly ISearchCacheUpdater searchCacheUpdater;
        private readonly ITask translateTask;
        private readonly ITask notificationTask;

        public SyncTask(ILogger<SyncTask> logger, IDbProvider dbProvider, DataParser dataParser, SyncSchedulerService syncScheduler, ISearchCacheUpdater searchCacheUpdater, ITask<TranslateTask> translateTask, ITask<NotificationTask> notificationTask)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
            this.syncScheduler = syncScheduler ?? throw new ArgumentNullException(nameof(syncScheduler));
            this.searchCacheUpdater = searchCacheUpdater;
            this.translateTask = translateTask;
            this.notificationTask = notificationTask;
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            // Init TonClient and retry if needed, before actually syncing entities.
            currentTask.Options.Interval = GetTaskDelay(currentTask.RunStatus.FailsCount);

            var db = dbProvider.MainDb;

            var lastSeqno = db.Find<Settings>(Settings.LAST_SEQNO);

            await dataParser.EnsureSynced(lastSeqno?.LongValue ?? 0);

            var counter = 0;
            while (counter < MaxBatch)
            {
                var next = db.Table<SyncQueueItem>().OrderBy(x => x.SyncAt).FirstOrDefault();

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

                    var lastSync = await task;

                    var deleted = db.Table<SyncQueueItem>().Where(x => x.Index == next.Index && x.EntityType == next.EntityType && x.MinLastSync <= lastSync).Delete();

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
                            db.InsertOrReplace(next);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var delay = GetDelay(next.RetryCount);
                    logger.LogError(ex, "Sync #{Counter} ({Type} #{Index}) failed, will retry in {Delay}.", counter, next.EntityType, next.Index, delay);
                    next.SyncAt = DateTimeOffset.UtcNow + delay;
                    next.RetryCount += 1;
                    db.Update(next);
                }
            }

            if (counter > 0)
            {
                await searchCacheUpdater.UpdateSearchCache();
                translateTask.TryRunImmediately();
                notificationTask.TryRunImmediately();
            }
        }

        protected async Task<DateTimeOffset> SyncAdmin(long index)
        {
            var admin = dbProvider.MainDb.Table<Admin>().FirstOrDefault(x => x.Index == index);
            if (admin == null)
            {
                logger.LogWarning("Admin #{Index} was not found, nothing to sync", index);
                return DateTimeOffset.MaxValue;
            }

            await dataParser.UpdateAdmin(admin);
            dbProvider.MainDb.InsertOrReplace(admin);
            return admin.LastSync;
        }

        protected async Task<DateTimeOffset> SyncUser(long index)
        {
            var user = dbProvider.MainDb.Table<User>().FirstOrDefault(x => x.Index == index);
            if (user == null)
            {
                logger.LogWarning("User #{Index} was not found, nothing to sync", index);
                return DateTimeOffset.MaxValue;
            }

            var changed = await dataParser.UpdateUser(user);

            if (changed)
            {
                user.AboutHash = string.IsNullOrWhiteSpace(user.About) ? null : SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(user.About));
                user.NeedTranslation = true;
            }

            dbProvider.MainDb.InsertOrReplace(user);
            return user.LastSync;
        }

        protected async Task<DateTimeOffset> SyncOrder(long index)
        {
            var order = dbProvider.MainDb.Table<Order>().FirstOrDefault(x => x.Index == index);
            if (order == null)
            {
                logger.LogWarning("Order #{Index} was not found, nothing to sync", index);
                return DateTimeOffset.MaxValue;
            }

            var endLt = order.LastTxLt;
            var changed = await dataParser.UpdateOrder(order);

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

                var exist = dbProvider.MainDb.Table<OrderActivity>().Count(x => x.OrderId == order.Id && x.TxLt == activity.TxLt);
                if (exist == 0)
                {
                    logger.LogDebug("Tx for Order {Address} added: Op {OpCode} at {Time} ({Lt}/{Hash})", order.Address, activity.OpCode, activity.Timestamp, activity.TxLt, activity.TxHash);
                    dbProvider.MainDb.Insert(activity);
                    dbProvider.MainDb.Insert(new NotificationQueueItem(activity.Id, activity.Timestamp));
                }
                else
                {
                    logger.LogDebug("Tx for Order {Address} already exists: Op {OpCode} at {Time} ({Lt}/{Hash})", order.Address, activity.OpCode, activity.Timestamp, activity.TxLt, activity.TxHash);
                }
            }

            dbProvider.MainDb.InsertOrReplace(order);

            return order.LastSync;
        }

        protected async Task<DateTimeOffset> SyncMaster()
        {
            var db = dbProvider.MainDb;
            var master = db.Find<Settings>(Settings.MASTER_ADDRESS);
            var masterAddress = master.StringValue!;

            var md = await dataParser.ParseMasterData(masterAddress);

            // Create missing Admins
            var nextAdmin = db.Find<Settings>(Settings.NEXT_INDEX_ADMIN)?.LongValue ?? 0;
            if (nextAdmin < md.nextAdminIndex)
            {
                await foreach (var (index, address) in dataParser.EnumerateAdminAddresses(masterAddress, nextAdmin, md.nextAdminIndex))
                {
                    var entity = db.Find<Admin>(x => x.Index == index);
                    if (entity == null)
                    {
                        entity = new Admin()
                        {
                            Index = index,
                            Address = TonUtils.Address.SetBounceable(address, true),
                            AdminAddress = masterAddress,
                        };
                        db.Insert(entity);
                        syncScheduler.Schedule(entity);
                        logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                    }
                }

                db.InsertOrReplace(new Settings(Settings.NEXT_INDEX_ADMIN, md.nextAdminIndex));
            }

            // Create missing Users
            var nextUser = db.Find<Settings>(Settings.NEXT_INDEX_USER)?.LongValue ?? 0;
            if (nextUser < md.nextUserIndex)
            {
                await foreach (var (index, address) in dataParser.EnumerateUserAddresses(masterAddress, nextUser, md.nextUserIndex))
                {
                    var entity = db.Find<User>(x => x.Index == index);
                    if (entity == null)
                    {
                        entity = new User()
                        {
                            Index = index,
                            Address = TonUtils.Address.SetBounceable(address, true),
                            UserAddress = masterAddress,
                        };
                        db.Insert(entity);
                        syncScheduler.Schedule(entity);
                        logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                    }
                }

                db.InsertOrReplace(new Settings(Settings.NEXT_INDEX_USER, md.nextUserIndex));
            }

            // Create missing Orders
            var nextOrder = db.Find<Settings>(Settings.NEXT_INDEX_ORDER)?.LongValue ?? 0;
            if (nextOrder < md.nextOrderIndex)
            {
                await foreach (var (index, address) in dataParser.EnumerateOrderAddresses(masterAddress, nextOrder, md.nextOrderIndex))
                {
                    var entity = db.Find<Order>(x => x.Index == index);
                    if (entity == null)
                    {
                        entity = new Order()
                        {
                            Index = index,
                            Address = TonUtils.Address.SetBounceable(address, true),
                            CustomerAddress = masterAddress,
                        };
                        db.Insert(entity);
                        syncScheduler.Schedule(entity);
                        logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                    }
                }

                db.InsertOrReplace(new Settings(Settings.NEXT_INDEX_ORDER, md.nextOrderIndex));
            }

            // Recreate all categories
            db.Table<Category>().Delete(x => true);
            var catCount = db.InsertAll(md.categories);
            logger.LogDebug("Reloaded {Count} categories", catCount);

            // Recreate all languages
            var langOldCount = db.Table<Language>().Delete(x => true);
            var langNewCount = db.InsertAll(md.languages);
            logger.LogDebug("Reloaded {Count} languages", langNewCount);
            if (langOldCount != langNewCount)
            {
                var c1 = db.Execute($"UPDATE [{nameof(Order)}] SET {nameof(Order.NeedTranslation)} = 1");
                var c2 = db.Execute($"UPDATE [{nameof(User)}] SET {nameof(User.NeedTranslation)} = 1");
                logger.LogDebug("A number of languages changed, so {Count} orders and {Count} users were marked for re-translation", c1, c2);
            }

            // Check TX history and re-sync contracts
            var endLt = dbProvider.MainDb.Find<Settings>(Settings.LAST_MASTER_TX_LT);
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

                        var a = db.Find<Admin>(x => x.Address == adr);
                        if (a != null)
                        {
                            syncScheduler.Schedule(a);
                            found++;
                        }

                        var u = db.Find<User>(x => x.Address == adr);
                        if (u != null)
                        {
                            syncScheduler.Schedule(u);
                            found++;
                        }

                        var o = db.Find<Order>(x => x.Address == adr);
                        if (o != null)
                        {
                            syncScheduler.Schedule(o);
                            found++;
                        }
                    }
                }

                logger.LogDebug("Checked transactions to Lt={Lt}, found {Count} known addresses (non unique!) for sync.", md.lastTx.Lt, found);
            }

            db.InsertOrReplace(new Settings(Settings.LAST_MASTER_DATA_HASH, md.stateHash));
            db.InsertOrReplace(new Settings(Settings.LAST_MASTER_TX_LT, md.lastTx.Lt));

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
