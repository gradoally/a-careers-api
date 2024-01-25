using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class SearchService : IRunnable
    {
        public const string OrderAsc = "asc";
        public const string OrderDesc = "desc";

        private readonly ILogger logger;
        private readonly BackendOptions options;

        private List<Admin> admins = new();
        private List<User> users = new();
        private List<Order> orders = new();

        public SearchService(ILogger<SearchService> logger, IOptions<BackendOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            var newAdmins = await db.MainDb.Table<Admin>().ToListAsync().ConfigureAwait(false);
            logger.LogDebug("Loaded {Count} admins", newAdmins.Count);

            var newUsers = await db.MainDb.Table<User>().ToListAsync().ConfigureAwait(false);
            logger.LogDebug("Loaded {Count} users", newUsers.Count);

            var newOrders = await db.MainDb.Table<Order>().ToListAsync().ConfigureAwait(false);
            logger.LogDebug("Loaded {Count} orders", newOrders.Count);

            admins = newAdmins;
            users = newUsers;
            orders = newOrders;
        }

        public User? FindUser(string addressAsBounceable)
        {
            return users.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, addressAsBounceable));
        }
    }
}
