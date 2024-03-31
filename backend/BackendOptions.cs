namespace SomeDAO.Backend
{
    public class BackendOptions
    {
        public string MasterAddress { get; set; } = string.Empty;

        public string DeeplToken { get; set; } = string.Empty;

        public string DatabaseFile { get; set; } = "./backend.sqlite";

        public string? NotificationsEndpoint { get; set; }

        public string CacheDirectory { get; set; } = "./cache";

        public TimeSpan SearchCacheForceReloadInterval { get; set; } = TimeSpan.FromMinutes(5);

        public TimeSpan MasterSyncInterval { get; set; } = TimeSpan.FromSeconds(10);

        public TimeSpan AdminForceResyncInterval { get; set; } = TimeSpan.FromDays(1);

        public TimeSpan UserForceResyncInterval { get; set; } = TimeSpan.FromDays(1);

        public TimeSpan OrderForceResyncInterval { get; set; } = TimeSpan.FromDays(1);

        public PathString SearchCacheUpdatePath { get; set; } = "/update-search-cache";

        public PathString HealthReportPath { get; set; } = "/update-indexer-health-data";

        public TimeSpan IndexerSubprocessRestartInterval { get; set; } = TimeSpan.FromHours(2);
    }
}
