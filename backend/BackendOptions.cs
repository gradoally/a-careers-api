namespace SomeDAO.Backend
{
    public class BackendOptions
    {
        public string MasterAddress { get; set; } = string.Empty;

        public bool UseMainnet { get; set; } = true;

        public string DatabaseFile { get; set; } = "./backend.sqlite";

        public string CacheDirectory { get; set; } = "./cache";

        //public TimeSpan NewOrdersDetectorInterval { get; set; } = TimeSpan.FromMinutes(15);

        //public TimeSpan CollectionTxTrackingInterval { get; set; } = TimeSpan.FromSeconds(10);

        //public TimeSpan MasterTxTrackingInterval { get; set; } = TimeSpan.FromSeconds(10);

        //public TimeSpan OrderUpdateCheckerInterval { get; set; } = TimeSpan.FromMinutes(5);

        public TimeSpan SearchCacheForceReloadInterval { get; set; } = TimeSpan.FromMinutes(5);

        public TimeSpan AdminForceResyncInterval { get; set; } = TimeSpan.FromHours(1);

        public TimeSpan UserForceResyncInterval { get; set; } = TimeSpan.FromHours(1);

        public TimeSpan OrderForceResyncInterval { get; set; } = TimeSpan.FromHours(2);

        //public Uri TonApiMainnetEndoint { get; set; } = new Uri("http://localhost");

        //public Uri TonApiTestnetEndoint { get; set; } = new Uri("http://localhost");

        //public int SearchMaxCount { get; set; } = 100;

        public Dictionary<string, string> Categories { get; set; } = new();
    }
}
