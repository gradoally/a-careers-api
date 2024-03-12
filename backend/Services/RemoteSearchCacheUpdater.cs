namespace SomeDAO.Backend.Services
{
    public class RemoteSearchCacheUpdater : ISearchCacheUpdater
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;

        public RemoteSearchCacheUpdater(ILogger<RemoteSearchCacheUpdater> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task UpdateSearchCache()
        {
            var baseUrl = configuration["Kestrel:Endpoints:Http:Url"];
            var path = configuration["BackendOptions:SearchCacheUpdatePath"];
            var uri = baseUrl + path;

            logger.LogDebug("Sending GET to {Uri}", uri);

            using var httpClient = httpClientFactory.CreateClient();
            using var resp = await httpClient.GetAsync(uri);
            resp.EnsureSuccessStatusCode();
        }
    }
}
