using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
	public class WIPService : IRunnable
	{
		public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

		private readonly ILogger logger;
		private readonly ITonClient tonClient;
		private readonly IDbProvider dbProvider;
		private readonly DataParser dataParser;
		private readonly ITask task;

		public WIPService(ILogger<WIPService> logger, ITonClient tonClient, IDbProvider dbProvider, DataParser dataParser, ITask<SearchService> task)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
			this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
			this.dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
			this.task = task ?? throw new ArgumentNullException(nameof(task));
		}

		public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
		{
			await tonClient.InitIfNeeded().ConfigureAwait(false);

			var db = dbProvider.MainDb;

			var admin = await dataParser.GetAdmin("EQDMMZ8op6c6G9tMNVgLs0-DkijEztCmTkPGd18RhYnjt76F");
			await db.InsertOrReplaceAsync(admin).ConfigureAwait(false);

			var user = await dataParser.GetUser("EQCk_PlzthTcQWbOXe_bhbWs6nsgwAWOdwF-mwHTF5vBYO0s");
			await db.InsertOrReplaceAsync(user).ConfigureAwait(false);

			var order = await dataParser.GetOrder("EQAup_18ePpROCkOKYj6o3IBrElsq5osmgAj-_276gPWHNQi");
			await db.InsertOrReplaceAsync(order).ConfigureAwait(false);

			logger.LogInformation("Initial data saved into DB");

			currentTask.Options.Interval = TimeSpan.Zero;

			task.TryRunImmediately();
		}
	}
}
