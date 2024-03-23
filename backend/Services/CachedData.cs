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

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            var master = db.MainDb.Find<Settings>(Settings.MASTER_ADDRESS)?.StringValue ?? string.Empty;
            var mainnet = db.MainDb.Find<Settings>(Settings.IN_MAINNET)?.BoolValue ?? false;
            var seqno = db.MainDb.Find<Settings>(Settings.LAST_SEQNO)?.LongValue ?? 0;

            var admins = db.MainDb.Table<Admin>().ToList();
            logger.LogTrace("Loaded {Count} admins", admins.Count);

            var users = db.MainDb.Table<User>().ToList();
            logger.LogTrace("Loaded {Count} users", users.Count);

            var orders = db.MainDb.Table<Order>().ToList();
            var activeOrders = orders.Where(x => x.Status == Order.status_active).ToList();
            logger.LogTrace("Loaded {Count} orders (including {Count} active)", orders.Count, activeOrders.Count);

            foreach (var order in orders)
            {
                order.Customer = users.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, order.CustomerAddress));
                order.Freelancer = users.Find(x => StringComparer.Ordinal.Equals(x.UserAddress, order.FreelancerAddress));
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
            AllAdmins = admins;
            AllUsers = users;
            AllOrders = orders;
            ActiveOrders = activeOrders;
            AllCategories = categories;
            AllLanguages = languages;
            ActiveOrdersTranslated = activeOrdersTranslated;

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
