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

        private List<Order> items = new();

        public SearchService(ILogger<SearchService> logger, IOptions<BackendOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            var list = await db.MainDb.Table<Order>().ToListAsync().ConfigureAwait(false);

            logger.LogDebug("Loaded {Count} items", list.Count);

            items = list;
        }

        public int Count => items.Count;

        public List<Order> Find(string? query, string? status, string? category, decimal? minAmount, decimal? maxAmount, string orderByField, string orderBySort)
        {
            var found = items.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                found = found.Where(x => string.Equals(x.Status, status, StringComparison.InvariantCultureIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                found = found.Where(x => string.Equals(x.Category, category, StringComparison.InvariantCultureIgnoreCase));
            }

            if (minAmount != null)
            {
                found = found.Where(x => x.Amount >= minAmount);
            }

            if (maxAmount != null)
            {
                found = found.Where(x => x.Amount <= maxAmount);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var words = query.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                found = found.Where(x => Array.TrueForAll(words, z => x.TextToSearch.Contains(z, StringComparison.InvariantCulture)));
            }

            var orderAsc = orderBySort == SearchService.OrderAsc;

            var ordered = orderByField switch
            {
                DataParser.PropNameStartUnixTime => orderAsc ? found.OrderBy(x => x.Starting) : found.OrderByDescending(x => x.Starting),
                DataParser.PropNameEndUnixTime => orderAsc ? found.OrderBy(x => x.Ending) : found.OrderByDescending(x => x.Ending),
                _ => orderAsc ? found.OrderBy(x => x.Created) : found.OrderByDescending(x => x.Created),
            };

            return ordered.ThenBy(x => x.Index).Take(options.SearchMaxCount).ToList();
        }
    }
}
