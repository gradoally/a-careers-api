using Microsoft.Extensions.Options;

namespace SomeDAO.Backend.Services
{
    public class RemoteSearchCacheUpdater : ISearchCacheUpdater
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private readonly string searchCacheUpdatePath;
        private readonly IHttpClientFactory httpClientFactory;

        public RemoteSearchCacheUpdater(ILogger<RemoteSearchCacheUpdater> logger, IConfiguration configuration, IOptions<BackendOptions> backendOptions, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.searchCacheUpdatePath = backendOptions.Value.SearchCacheUpdatePath;
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task UpdateSearchCache()
        {
            var baseUrl = configuration["Kestrel:Endpoints:Http:Url"];
            var uri = baseUrl + searchCacheUpdatePath;

            logger.LogDebug("Sending GET to {Uri}", uri);

            using var httpClient = httpClientFactory.CreateClient();
            using var resp = await httpClient.GetAsync(uri);
            resp.EnsureSuccessStatusCode();
        }
    }
}
