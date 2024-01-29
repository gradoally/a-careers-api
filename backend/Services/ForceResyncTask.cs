using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
	public class ForceResyncTask : IRunnable
	{
		public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

		private readonly ILogger logger;
		private readonly BackendOptions options;
		private readonly IDbProvider dbProvider;
		private readonly ITask syncTask;

		public ForceResyncTask(ILogger<ForceResyncTask> logger, IOptions<BackendOptions> options, IDbProvider dbProvider, ITask<SyncTask> syncTask)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
			this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
			this.syncTask = syncTask ?? throw new ArgumentNullException(nameof(syncTask));
		}

		public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
		{
			var count = 0;

			count += await ForceResyncAdmins();
			count += await ForceResyncUsers();
			count += await ForceResyncOrders();

			if (count > 0)
			{
				syncTask.TryRunImmediately();
			}
		}

		protected async Task<int> ForceResyncAdmins()
		{
			var boundary = DateTimeOffset.UtcNow.Subtract(options.AdminForceResyncInterval);
			var list = await dbProvider.MainDb.Table<Admin>().Where(x => x.LastSync < boundary).ToListAsync().ConfigureAwait(false);

			if (list.Count > 0)
			{
				var queue = list.Select(x => new SyncQueueItem(x)).ToList();
				await dbProvider.MainDb.InsertAllAsync(queue).ConfigureAwait(false);
			}

			logger.LogDebug("Scheduled {Count} Admins for resync", list.Count);

			return list.Count;
		}

		protected async Task<int> ForceResyncUsers()
		{
			var boundary = DateTimeOffset.UtcNow.Subtract(options.UserForceResyncInterval);
			var list = await dbProvider.MainDb.Table<User>().Where(x => x.LastSync < boundary).ToListAsync().ConfigureAwait(false);

			if (list.Count > 0)
			{
				var queue = list.Select(x => new SyncQueueItem(x)).ToList();
				await dbProvider.MainDb.InsertAllAsync(queue).ConfigureAwait(false);
			}

			logger.LogDebug("Scheduled {Count} Users for resync", list.Count);

			return list.Count;
		}

		protected async Task<int> ForceResyncOrders()
		{
			var boundary = DateTimeOffset.UtcNow.Subtract(options.OrderForceResyncInterval);
			var list = await dbProvider.MainDb.Table<Order>().Where(x => x.LastSync < boundary).ToListAsync().ConfigureAwait(false);

			if (list.Count > 0)
			{
				var queue = list.Select(x => new SyncQueueItem(x)).ToList();
				await dbProvider.MainDb.InsertAllAsync(queue).ConfigureAwait(false);
			}

			logger.LogDebug("Scheduled {Count} Orders for resync", list.Count);

			return list.Count;
		}
	}
}
