using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;
using TonLibDotNet.Utils;

namespace SomeDAO.Backend.Services
{
    public class CollectionTxTrackerService : IRunnable
    {
        private readonly int maxPages = 10;

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly HttpClient httpClient;
        private readonly BackendOptions options;

        public CollectionTxTrackerService(ILogger<CollectionTxTrackerService> logger, IDbProvider dbProvider, HttpClient httpClient, IOptions<BackendOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var lastLtSettings = await dbProvider.MainDb.FindAsync<Settings>(Settings.LAST_COLLECTION_TX_LT).ConfigureAwait(false);
            var lastLt = lastLtSettings?.LongValue ?? 0;

            var before = long.MaxValue;
            var maxLt = lastLt;
            var pages = 0;
            var txs = 0;
            var bounced = 0;
            var unknownCount = 0;
            var updatedCount = 0;

            while (pages < maxPages)
            {
                pages++;

                // do not wait on first and second requests (faster)
                if (pages > 2)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                }

                var uri = new Uri(
                    options.UseMainnet ? options.TonApiMainnetEndoint : options.TonApiTestnetEndoint,
                    $"/v2/blockchain/accounts/{options.CollectionAddress}/transactions?after_lt={lastLt}&before_lt={before}");
                var resp = await httpClient.GetFromJsonAsync<AccountTransactions>(uri, cancellationToken);

                if (resp == null || resp.transactions == null || resp.transactions.Length == 0)
                {
                    break;
                }

                foreach(var tx in resp.transactions)
                {
                    before = Math.Min(before, tx.lt);
                    maxLt = Math.Max(maxLt, tx.lt);

                    txs++;

                    var hash = tx.hash;
                    var lt = tx.lt;
                    var from = tx.in_msg.source.address;
                    var fromBytes = Convert.FromHexString(from.Split(':', 2)[1]);
                    var fromBnc = AddressValidator.MakeAddress(0, fromBytes);
                    var time = DateTimeOffset.FromUnixTimeSeconds(tx.utime);

                    if (tx.in_msg.bounced)
                    {
                        logger.LogDebug("Tx from {Adress} is bounced, skipping ({Hash} / {LT})", from, hash, lt);
                        bounced++;
                        continue;
                    }

                    var nft = await dbProvider.MainDb.Table<Order>().FirstOrDefaultAsync(x => x.Address == fromBnc);
                    if (nft == null)
                    {
                        logger.LogInformation("Tx from unknown address {Address}, will start NewOrderDetector... ({Hash} / {LT})", from, hash, lt);
                        unknownCount++;
                    }
                    else
                    {
                        if (nft.LastUpdate < time)
                        {
                            nft.UpdateAfter = time;
                            await dbProvider.MainDb.UpdateAsync(nft).ConfigureAwait(false);
                            updatedCount++;
                            logger.LogInformation("Order {Address} queued for update ({Hash} / {LT})", from, hash, lt);
                        }
                        else
                        {
                            logger.LogDebug("Order {Address} is already up-to-date, skipped ({Hash} / {LT})", from, hash, lt);
                        }
                    }
                }
            }

            await dbProvider.MainDb.InsertOrReplaceAsync(new Settings(Settings.LAST_COLLECTION_TX_LT, maxLt)).ConfigureAwait(false);

            logger.LogInformation("Done. Processed {Count} pages with {Count} transactions (incl. {Count} bounced), found {Count} unknown and {Count} updated addresses, new lt={Value}", pages, txs, bounced, unknownCount, updatedCount, maxLt);

            if (updatedCount > 0)
            {
                scopeServiceProvider.GetRequiredService<ITask<OrderUpdateChecker>>().TryRunImmediately();
            }

            if (unknownCount > 0)
            {
                scopeServiceProvider.GetRequiredService<ITask<NewOrdersDetector>>().TryRunImmediately();
            }
        }

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public class AccountTransactions
        {
            public Transaction[]? transactions { get; set; }
        }

        public class Transaction
        {
            public string hash { get; set; }
            public long lt { get; set; }
            public long utime { get; set; }
            public InMsg in_msg { get; set; }
            public string prev_trans_hash { get; set; }
            public long prev_trans_lt { get; set; }
        }

        public class InMsg
        {
            public long created_lt { get; set; }
            public bool bounced { get; set; }
            public int value { get; set; }
            public Source source { get; set; }
            public int created_at { get; set; }
            public string op_code { get; set; }
        }

        public class Source
        {
            public string address { get; set; }
        }
    }
}
