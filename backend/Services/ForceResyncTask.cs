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
			var c1 = await ForceResyncAdmins();
			var c2 = await ForceResyncUsers();
			var c3 = await ForceResyncOrders();

			logger.LogDebug("Scheduled {Count} Admins, {Count} Users, {Count} Orders for resync", c1, c2, c3);

			if (c1 != 0 || c2 != 0 || c3 != 0)
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

			return list.Count;
		}
	}
}
