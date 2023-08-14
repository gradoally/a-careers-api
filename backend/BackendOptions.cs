namespace SomeDAO.Backend
{
    public class BackendOptions
    {
        public string CollectionAddress { get; set; } = string.Empty;

        public bool UseMainnet { get; set; } = true;

        public string DatabaseFile { get; set; } = "./backend.sqlite";

        public string CacheDirectory { get; set; } = "./cache";

        public TimeSpan NewItemDetectorInterval { get; set; } = TimeSpan.FromMinutes(15);
    }
}
