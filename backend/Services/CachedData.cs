using System.Linq;
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
        public List<Order> ActiveOrders { get; private set; } = new();
        public List<Category> AllCategories { get; private set; } = new();
        public List<Language> AllLanguages { get; private set; } = new();
        public Dictionary<string, List<Order>> ActiveOrdersTranslated { get; private set; } = new();

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            var admins = await db.MainDb.Table<Admin>().ToListAsync().ConfigureAwait(false);
            logger.LogTrace("Loaded {Count} admins", admins.Count);

            var users = await db.MainDb.Table<User>().ToListAsync().ConfigureAwait(false);
            logger.LogTrace("Loaded {Count} users", users.Count);

            var orders = await db.MainDb.Table<Order>().ToListAsync().ConfigureAwait(false);
            var activeOrders = orders.Where(x => x.Status == OrderStatus.Active).ToList();
            logger.LogTrace("Loaded {Count} orders (including {Count} active)", orders.Count, activeOrders.Count);

            foreach (var order in orders)
            {
                order.Customer = users.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, order.CustomerAddress));
                order.Freelancer = users.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, order.FreelancerAddress));
            }

            logger.LogTrace("Users applied to Orders");

            var categories = await db.MainDb.Table<Category>().ToListAsync().ConfigureAwait(false);
            logger.LogTrace("Loaded {Count} categories", categories.Count);

            var languages = await db.MainDb.Table<Language>().ToListAsync().ConfigureAwait(false);
            logger.LogTrace("Loaded {Count} languages", languages.Count);

            var hashes = activeOrders.Select(x => x.NameHash)
                .Concat(activeOrders.Select(x => x.DescriptionHash))
                .Concat(activeOrders.Select(x => x.TechnicalTaskHash))
                .Where(x => x != null)
                .Distinct()
                .ToArray();
            var activeOrdersTranslated = new Dictionary<string, List<Order>>(StringComparer.OrdinalIgnoreCase);
            foreach (var language in languages)
            {
                var translations = await db.MainDb.Table<Translation>().Where(x => x.Language == language.Name && hashes.Contains(x.Hash)).ToListAsync().ConfigureAwait(false);
                var copies = activeOrders.Select(x => x.ShallowCopy()).ToList();
                foreach (var item in copies)
                {
                    if (item.NameHash != null)
                    {
                        item.NameTranslated = translations.Find(x => x.Hash.SequenceEqual(item.NameHash))?.TranslatedText;
                    }

                    if (item.DescriptionHash != null)
                    {
                        item.DescriptionTranslated = translations.Find(x => x.Hash.SequenceEqual(item.DescriptionHash))?.TranslatedText;
                    }

                    if (item.TechnicalTaskHash != null)
                    {
                        item.TechnicalTaskTranslated = translations.Find(x => x.Hash.SequenceEqual(item.TechnicalTaskHash))?.TranslatedText;
                    }
                }
                activeOrdersTranslated[language.Hash] = copies;
                activeOrdersTranslated[language.Name] = copies;
            }
            logger.LogTrace("Translations applied to ActiveOrders");

            AllAdmins = admins;
            AllUsers = users;
            AllOrders = orders;
            ActiveOrders = activeOrders;
            AllCategories = categories;
            AllLanguages = languages;
            ActiveOrdersTranslated = activeOrdersTranslated;

            logger.LogDebug(
                "Reloaded {Count} admins, {Count} users, {Count} orders (incl. {Count} active), {Count} categories, {Count} languages.",
                AllAdmins.Count,
                AllUsers.Count,
                AllOrders.Count,
                ActiveOrders.Count,
                AllCategories.Count,
                AllLanguages.Count);
        }
    }
}
