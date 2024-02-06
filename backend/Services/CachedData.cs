using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public class CachedData : IRunnable
    {
        public const string OrderAsc = "asc";
        public const string OrderDesc = "desc";

        private readonly ILogger logger;

        public CachedData(ILogger<CachedData> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Admin> AllAdmins { get; private set; } = new();
        public List<User> AllUsers { get; private set; } = new();
        public List<Order> AllOrders { get; private set; } = new();
        public List<Order> AllActiveOrders { get; private set; } = new();

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            var admins = await db.MainDb.Table<Admin>().ToListAsync().ConfigureAwait(false);
            logger.LogDebug("Loaded {Count} admins", admins.Count);

            var users = await db.MainDb.Table<User>().ToListAsync().ConfigureAwait(false);
            logger.LogDebug("Loaded {Count} users", users.Count);

            var orders = await db.MainDb.Table<Order>().ToListAsync().ConfigureAwait(false);
            var activeOrders = orders.Where(x => x.Status == OrderStatus.Active).ToList();
            logger.LogDebug("Loaded {Count} orders (including {Count} active)", orders.Count, activeOrders.Count);

            foreach (var order in orders)
            {
                order.Customer = users.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, order.CustomerAddress));
                order.Freelancer = users.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, order.FreelancerAddress));
            }

            logger.LogDebug("Users applied to Orders");

            AllAdmins = admins;
            AllUsers = users;
            AllOrders = orders;
            AllActiveOrders = activeOrders;
        }
    }
}
