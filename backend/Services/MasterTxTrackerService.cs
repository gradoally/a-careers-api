using Microsoft.Extensions.Options;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public class MasterTxTrackerService : TxTrackerServiceBase
    {
        public MasterTxTrackerService(ILogger<MasterTxTrackerService> logger, IDbProvider dbProvider, HttpClient httpClient, IOptions<BackendOptions> options)
            : base(logger, dbProvider, httpClient, options)
        {
            SettingsName = Settings.LAST_MASTER_TX_LT;
            AccountAddress = options.Value.MasterAddress;
            IgnoreUnknown = true;
        }
    }
}
