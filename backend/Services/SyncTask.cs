using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

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
        private readonly ITask cachedDataTask;

        public SyncTask(ILogger<SyncTask> logger, IDbProvider dbProvider, DataParser dataParser, SyncSchedulerService syncScheduler, ITask<CachedData> cachedDataTask)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
            this.syncScheduler = syncScheduler ?? throw new ArgumentNullException(nameof(syncScheduler));
            this.cachedDataTask = cachedDataTask;
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

            await dataParser.UpdateUser(user).ConfigureAwait(false);
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
            await dataParser.UpdateOrder(order).ConfigureAwait(false);

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
                await foreach (var item in dataParser.EnumerateAdminAddresses(masterAddress, nextAdmin, md.nextAdminIndex))
                {
                    var entity = new Admin()
                    {
                        Index = item.index,
                        Address = TonUtils.Address.SetBounceable(item.address, true),
                        AdminAddress = masterAddress,
                    };
                    await db.InsertAsync(entity).ConfigureAwait(false);
                    await syncScheduler.Schedule(entity);
                    logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                }

                await db.InsertOrReplaceAsync(new Settings(Settings.NEXT_INDEX_ADMIN, md.nextAdminIndex)).ConfigureAwait(false);
            }

            // Create missing Users
            var nextUser = (await db.FindAsync<Settings>(Settings.NEXT_INDEX_USER))?.LongValue ?? 0;
            if (nextUser < md.nextUserIndex)
            {
                await foreach (var item in dataParser.EnumerateUserAddresses(masterAddress, nextUser, md.nextUserIndex))
                {
                    var entity = new User()
                    {
                        Index = item.index,
                        Address = TonUtils.Address.SetBounceable(item.address, true),
                        UserAddress = masterAddress,
                    };
                    await db.InsertAsync(entity).ConfigureAwait(false);
                    await syncScheduler.Schedule(entity);
                    logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                }

                await db.InsertOrReplaceAsync(new Settings(Settings.NEXT_INDEX_USER, md.nextUserIndex)).ConfigureAwait(false);
            }

            // Create missing Orders
            var nextOrder = (await db.FindAsync<Settings>(Settings.NEXT_INDEX_ORDER))?.LongValue ?? 0;
            if (nextOrder < md.nextOrderIndex)
            {
                await foreach (var item in dataParser.EnumerateOrderAddresses(masterAddress, nextOrder, md.nextOrderIndex))
                {
                    var entity = new Order()
                    {
                        Index = item.index,
                        Address = TonUtils.Address.SetBounceable(item.address, true),
                        CustomerAddress = masterAddress,
                    };
                    await db.InsertAsync(entity).ConfigureAwait(false);
                    await syncScheduler.Schedule(entity);
                    logger.LogInformation("New {EntityType} #{Index} detected: {Address}", entity.EntityType, entity.Index, entity.Address);
                }

                await db.InsertOrReplaceAsync(new Settings(Settings.NEXT_INDEX_ORDER, md.nextOrderIndex)).ConfigureAwait(false);
            }

            // Recreate all categories
            await db.Table<Category>().DeleteAsync(x => true).ConfigureAwait(false);
            var catCount = await db.InsertAllAsync(md.categories).ConfigureAwait(false);
            logger.LogDebug("Reloaded {Count} categories", catCount);

            // Recreate all languages
            await db.Table<Language>().DeleteAsync(x => true).ConfigureAwait(false);
            var lanCount = await db.InsertAllAsync(md.languages).ConfigureAwait(false);
            logger.LogDebug("Reloaded {Count} languages", lanCount);

            await db.InsertOrReplaceAsync(new Settings(Settings.LAST_MASTER_DATA_HASH, md.hash)).ConfigureAwait(false);

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
