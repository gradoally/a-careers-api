namespace SomeDAO.Backend
{
    public class BackendOptions
    {
        public string CollectionAddress { get; set; } = string.Empty;

        public bool UseMainnet { get; set; } = true;

        public string DatabaseFile { get; set; } = "./backend.sqlite";

        public string CacheDirectory { get; set; } = "./cache";

        public TimeSpan NewItemDetectorInterval { get; set; } = TimeSpan.FromMinutes(15);

        public TimeSpan CollectionTxTrackingInterval { get; set; } = TimeSpan.FromSeconds(10);

        public TimeSpan ItemUpdateCheckerInterval { get; set; } = TimeSpan.FromMinutes(5);

        public TimeSpan SearchCacheForceReloadInterval { get; set; } = TimeSpan.FromMinutes(5);

        public Uri TonApiMainnetEndoint { get; set; } = new Uri("http://localhost");

        public Uri TonApiTestnetEndoint { get; set; } = new Uri("http://localhost");

        public int SearchMaxCount { get; set; } = 100;
    }
}
