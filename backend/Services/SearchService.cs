using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public class SearchService : IRunnable
    {
        public const string OrderAsc = "asc";
        public const string OrderDesc = "desc";

        private readonly ILogger logger;
        private readonly BackendOptions options;

		public SearchService(ILogger<SearchService> logger, IOptions<BackendOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

		public List<Admin> AllAdmins { get; private set; } = new();
		public List<User> AllUsers { get; private set; } = new();
		public List<Order> AllOrders { get; private set; } = new();

		public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            var admins = await db.MainDb.Table<Admin>().ToListAsync().ConfigureAwait(false);
            logger.LogDebug("Loaded {Count} admins", admins.Count);

            var users = await db.MainDb.Table<User>().ToListAsync().ConfigureAwait(false);
            logger.LogDebug("Loaded {Count} users", users.Count);

            var orders = await db.MainDb.Table<Order>().ToListAsync().ConfigureAwait(false);
            logger.LogDebug("Loaded {Count} orders", orders.Count);

            AllAdmins = admins;
            AllUsers = users;
            AllOrders = orders;
        }

		public List<Order> FindOrders(string? query, string? category, string? language, decimal? minPrice)
		{
			var found = AllOrders.AsQueryable();

			if (!string.IsNullOrWhiteSpace(category))
			{
				found = found.Where(x => string.Equals(x.Category, category, StringComparison.InvariantCultureIgnoreCase));
			}

			if (!string.IsNullOrWhiteSpace(language))
			{
				found = found.Where(x => string.Equals(x.Language, language, StringComparison.InvariantCultureIgnoreCase));
			}

			if (minPrice != null)
			{
				found = found.Where(x => x.Price >= minPrice);
			}

			if (!string.IsNullOrWhiteSpace(query))
			{
				var words = query.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
				found = found.Where(x => Array.TrueForAll(words, z => x.TextToSearch.Contains(z, StringComparison.InvariantCulture)));
			}

			return found.ToList();
		}
	}
}
