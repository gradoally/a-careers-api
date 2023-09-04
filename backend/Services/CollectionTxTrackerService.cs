using Microsoft.Extensions.Options;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public class CollectionTxTrackerService : TxTrackerServiceBase
    {
        public CollectionTxTrackerService(ILogger<CollectionTxTrackerService> logger, IDbProvider dbProvider, HttpClient httpClient, IOptions<BackendOptions> options)
            : base(logger, dbProvider, httpClient, options)
        {
            SettingsName = Settings.LAST_COLLECTION_TX_LT;
            AccountAddress = options.Value.CollectionAddress;
            IgnoreUnknown = false;
        }
    }
}
