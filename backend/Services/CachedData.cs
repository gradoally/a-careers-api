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

        public string MasterAddress { get; private set; } = string.Empty;
        public bool InMainnet { get; private set; }
        public long LastKnownSeqno { get; private set; }
        public List<Admin> AllAdmins { get; private set; } = new();
        public List<User> AllUsers { get; private set; } = new();
        public List<Order> AllOrders { get; private set; } = new();
        public List<Order> ActiveOrders { get; private set; } = new();
        public List<Category> AllCategories { get; private set; } = new();
        public List<Language> AllLanguages { get; private set; } = new();
        public Dictionary<string, List<Order>> ActiveOrdersTranslated { get; private set; } = new();
        public Dictionary<int, int> OrderCountByStatus { get; private set;} = new();
        public Dictionary<string, int> OrderCountByCategory { get; private set;} = new();
        public Dictionary<string, int> OrderCountByLanguage { get; private set;} = new();
        public Dictionary<string, int> UserCountByStatus { get; private set;} = new();
        public Dictionary<string, int> UserCountByLanguage { get; private set;} = new();

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            var master = db.MainDb.Find<Settings>(Settings.MASTER_ADDRESS)?.StringValue ?? string.Empty;
            var mainnet = db.MainDb.Find<Settings>(Settings.IN_MAINNET)?.BoolValue ?? false;
            var seqno = db.MainDb.Find<Settings>(Settings.LAST_SEQNO)?.LongValue ?? 0;

            var admins = db.MainDb.Table<Admin>().ToList();
            var adminsWithData = admins.Where(x => x.AdminAddress != master).ToList();
            logger.LogTrace("Loaded {Count} admins with data (of {Count} total)", adminsWithData.Count, admins.Count);

            var users = db.MainDb.Table<User>().ToList();
            var usersWithData = users.Where(x => x.UserAddress != master).ToList();
            logger.LogTrace("Loaded {Count} users with data (of {Count} total)", usersWithData.Count, users.Count);

            var orders = db.MainDb.Table<Order>().ToList();
            var ordersWithData = orders.Where(x => x.CustomerAddress != master).ToList();
            var activeOrders = ordersWithData.Where(x => x.Status == Order.status_active).ToList();
            logger.LogTrace("Loaded {Count} orders (including {Count} active) of {Count} total", ordersWithData.Count, activeOrders.Count, orders.Count);

            foreach (var order in ordersWithData)
            {
                order.Customer = usersWithData.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, order.CustomerAddress));
                order.Freelancer = usersWithData.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, order.FreelancerAddress));
            }

            logger.LogTrace("Users applied to Orders");

            var categories = db.MainDb.Table<Category>().ToList();
            logger.LogTrace("Loaded {Count} categories", categories.Count);

            var languages = db.MainDb.Table<Language>().ToList();
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
                var translations = db.MainDb.Table<Translation>().Where(x => x.Language == language.Name && hashes.Contains(x.Hash)).ToList();
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

            MasterAddress = master;
            InMainnet = mainnet;
            LastKnownSeqno = seqno;
            AllAdmins = adminsWithData;
            AllUsers = usersWithData;
            AllOrders = ordersWithData;
            ActiveOrders = activeOrders;
            AllCategories = categories;
            AllLanguages = languages;
            ActiveOrdersTranslated = activeOrdersTranslated;
            OrderCountByStatus = ordersWithData.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count());
            OrderCountByCategory = ordersWithData.Where(x => !string.IsNullOrEmpty(x.Category)).GroupBy(x => x.Category!).ToDictionary(x => x.Key, x => x.Count());
            OrderCountByLanguage = ordersWithData.Where(x => !string.IsNullOrEmpty(x.Language)).GroupBy(x => x.Language!).ToDictionary(x => x.Key, x => x.Count());
            UserCountByStatus = usersWithData.GroupBy(x => x.UserStatus).ToDictionary(x => x.Key, x => x.Count());
            UserCountByLanguage = usersWithData.Where(x => !string.IsNullOrEmpty(x.Language)).GroupBy(x => x.Language!).ToDictionary(x => x.Key, x => x.Count());

            logger.LogDebug(
                "Reloaded at {Seqno}: {Count} admins, {Count} users, {Count} orders (incl. {Count} active), {Count} categories, {Count} languages.",
                LastKnownSeqno,
                AllAdmins.Count,
                AllUsers.Count,
                AllOrders.Count,
                ActiveOrders.Count,
                AllCategories.Count,
                AllLanguages.Count);

            return Task.CompletedTask;
        }
    }
}
