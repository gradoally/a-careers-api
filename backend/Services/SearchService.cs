using System.Linq;
using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public class SearchService : IRunnable, ISearchService
    {
        private readonly ILogger logger;
        private readonly BackendOptions options;

        private List<NftItem> items = new();

        public SearchService(ILogger<SearchService> logger, IOptions<BackendOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            var list = await db.MainDb.Table<NftItem>().ToListAsync().ConfigureAwait(false);

            logger.LogDebug("Loaded {Count} items", list.Count);

            items = list;
        }

        public List<NftItem> Find(string text)
        {
            var found = items.Where(x => x.OwnerAddress != null && x.OwnerAddress.Contains(text, StringComparison.InvariantCultureIgnoreCase));
            var ordered = found.OrderByDescending(x => x.LastUpdate);

            return ordered.Take(options.SearchMaxCount).ToList();
        }
    }
}
