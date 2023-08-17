using SomeDAO.Backend.Data;
using TonLibDotNet;

namespace SomeDAO.Backend.Services
{
    public class DataParser : IDataParser
    {
        private readonly ILogger logger;
        private readonly ITonClient tonClient;

        public DataParser(ILogger<DataParser> logger, ITonClient tonClient)
        {
            this.logger = logger;
            this.tonClient = tonClient;
        }

        public async Task<NftItem?> GetNftItem(string address)
        {
            await tonClient.InitIfNeeded().ConfigureAwait(false);

            var state = await tonClient.RawGetAccountState(address).ConfigureAwait(false);

            var info = await TonRecipes.NFTs.GetNftData(tonClient, address).ConfigureAwait(false);

            var item = new NftItem
            {
                Index = (int)info.index,
                Address = address,
                OwnerAddress = info.ownerAddress,
                LastUpdate = DateTimeOffset.UtcNow,
                LastTxHash = state.LastTransactionId.Hash,
                LastTxLt = state.LastTransactionId.Lt,
                UpdateNeeded = false,
            };

            return item;
        }
    }
}
