using SomeDAO.Backend.Data;
using TonLibDotNet;
using TonLibDotNet.Cells;
using TonLibDotNet.Types.Internal;
using TonLibDotNet.Types.Raw;
using TonLibDotNet.Types.Smc;

namespace SomeDAO.Backend.Services
{
    public class DataParser
    {
        private static readonly string PropCategory = GetSHA256OfStringAsHex("category");
        private static readonly string PropCanApproveUser = GetSHA256OfStringAsHex("can_approve_user");
        private static readonly string PropCanRevokeUser = GetSHA256OfStringAsHex("can_revoke_user");
        private static readonly string PropNickname = GetSHA256OfStringAsHex("nickname");
        private static readonly string PropAbout = GetSHA256OfStringAsHex("about");
        private static readonly string PropWebsite = GetSHA256OfStringAsHex("website");
        private static readonly string PropPortfolio = GetSHA256OfStringAsHex("portfolio");
        private static readonly string PropResume = GetSHA256OfStringAsHex("resume");
        private static readonly string PropSpecialization = GetSHA256OfStringAsHex("specialization");
        private static readonly string PropIsUser = GetSHA256OfStringAsHex("is_user");
        private static readonly string PropIsFreelancer = GetSHA256OfStringAsHex("is_freelancer");
        private static readonly string PropTelegram = GetSHA256OfStringAsHex("telegram");
        private static readonly string PropName = GetSHA256OfStringAsHex("name");
        private static readonly string PropDescription = GetSHA256OfStringAsHex("description");
        private static readonly string PropTechnicalTask = GetSHA256OfStringAsHex("technical_task");
        private static readonly string PropLanguage = GetSHA256OfStringAsHex("language");
        private static readonly string PropText = GetSHA256OfStringAsHex("text");
        private static readonly string PropPrice = GetSHA256OfStringAsHex("price");
        private static readonly string PropDeadline = GetSHA256OfStringAsHex("deadline");
        private static readonly string PropResult = GetSHA256OfStringAsHex("result");

        private readonly ILogger logger;
        private readonly ITonClient tonClient;

        public DataParser(ILogger<DataParser> logger, ITonClient tonClient)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.tonClient = tonClient ?? throw new ArgumentNullException(nameof(tonClient));
        }

        public static string GetSHA256OfStringAsHex(string value)
        {
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
        }

        public static void FillAdminContent(IAdminContent value, Cell fromDict)
        {
            var dict = fromDict.ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)), x => x, StringComparer.Ordinal);
            if (dict == null)
            {
                return;
            }

            Slice? s;
            value.Category = dict.TryGetValue(PropCategory, out s) ? Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant() : default;
            value.CanApproveUser = dict.TryGetValue(PropCanApproveUser, out s) && s.LoadBit();
            value.CanRevokeUser = dict.TryGetValue(PropCanRevokeUser, out s) && s.LoadBit();
            value.Nickname = dict.TryGetValue(PropNickname, out s) ? s.LoadStringSnake(true) : default;
            value.About = dict.TryGetValue(PropAbout, out s) ? s.LoadStringSnake(true) : default;
            value.Website = dict.TryGetValue(PropWebsite, out s) ? s.LoadStringSnake(true) : default;
            value.Portfolio = dict.TryGetValue(PropPortfolio, out s) ? s.LoadStringSnake(true) : default;
            value.Resume = dict.TryGetValue(PropResume, out s) ? s.LoadStringSnake(true) : default;
            value.Specialization = dict.TryGetValue(PropSpecialization, out s) ? s.LoadStringSnake(true) : default;
        }

        public static void FillUserContent(IUserContent value, Cell fromDict)
        {
            var dict = fromDict.ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)), x => x, StringComparer.Ordinal);
            if (dict == null)
            {
                return;
            }

            Slice? s;
            value.IsUser = dict.TryGetValue(PropIsUser, out s) && s.LoadBit();
            value.IsFreelancer = dict.TryGetValue(PropIsFreelancer, out s) && s.LoadBit();
            value.Nickname = dict.TryGetValue(PropNickname, out s) ? s.LoadStringSnake(true) : default;
            value.Telegram = dict.TryGetValue(PropTelegram, out s) ? s.LoadStringSnake(true) : default;
            value.About = dict.TryGetValue(PropAbout, out s) ? s.LoadStringSnake(true) : default;
            value.Website = dict.TryGetValue(PropWebsite, out s) ? s.LoadStringSnake(true) : default;
            value.Portfolio = dict.TryGetValue(PropPortfolio, out s) ? s.LoadStringSnake(true) : default;
            value.Resume = dict.TryGetValue(PropResume, out s) ? s.LoadStringSnake(true) : default;
            value.Specialization = dict.TryGetValue(PropSpecialization, out s) ? s.LoadStringSnake(true) : default;
            value.Language = dict.TryGetValue(PropLanguage, out s) ? Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant() : default;
        }

        public static void FillOrderContent(IOrderContent value, Cell fromDict)
        {
            var dict = fromDict.ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)), x => x, StringComparer.Ordinal);
            if (dict == null)
            {
                return;
            }

            Slice? s;
            value.Category = dict.TryGetValue(PropCategory, out s) ? Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant() : default;
            value.Language = dict.TryGetValue(PropLanguage, out s) ? Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant() : default;
            value.Name = dict.TryGetValue(PropName, out s) ? s.LoadStringSnake(true) : default;
            value.Description = dict.TryGetValue(PropDescription, out s) ? s.LoadStringSnake(true) : default;
            value.TechnicalTask = dict.TryGetValue(PropTechnicalTask, out s) ? s.LoadStringSnake(true) : default;
        }

        public async Task<long> EnsureSynced(long lastKnownSeqno = 0)
        {
            await tonClient.InitIfNeeded();
            var blockId = await tonClient.Sync();
            logger.LogDebug("Synced to masterchain block {Seqno}.", blockId.Seqno);

            if (blockId.Seqno < lastKnownSeqno)
            {
                tonClient.Deinit();
                throw new SyncException($"Sync failed: seqno {blockId.Seqno} is less than last known {lastKnownSeqno}.");
            }

            return blockId.Seqno;
        }

        public async Task<bool> UpdateAdmin(Admin value)
        {
            await tonClient.InitIfNeeded();

            var state = await tonClient.RawGetAccountState(value.Address);

            value.LastSync = state.SyncUtime;

            if (state.LastTransactionId.Lt == value.LastTxLt && state.LastTransactionId.Hash == value.LastTxHash)
            {
                return false;
            }

            if (string.IsNullOrEmpty(state.Data))
            {
                throw new InvalidOperationException("State 'data' is empty (contract not yet deployed?), nothing to update");
            }

            value.LastTxLt = state.LastTransactionId.Lt;
            value.LastTxHash = state.LastTransactionId.Hash;

            var smc = await tonClient.SmcLoad(value.Address);
            var data = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_admin_data"));
            await tonClient.SmcForget(smc.Id);

            if (data.ExitCode != 0)
            {
                throw new NonZeroExitCodeException(data.ExitCode, value.Address, "get_admin_data");
            }

            // Method returns:
            // (int, int, slice, slice, int, cell) get_admin_data()
            // (storage::init?, storage::index, storage::master_address, storage::admin_address, storage::revoked_at, storage::content)

            var index = data.Stack[1].ToLong();
            if (index != value.Index)
            {
                throw new ApplicationException($"Admin index mismatch: {index} got, {value.Index} expected for {value.Address}.");
            }

            var adminAddress = data.Stack[3].ToBoc().RootCells[0].BeginRead().LoadAddressIntStd(false);
            var revokedAt = data.Stack[4].ToInt();
            var content = data.Stack[5].ToBoc();

            value.AdminAddress = adminAddress;
            value.RevokedAt = revokedAt == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(revokedAt);

            FillAdminContent(value, content.RootCells[0]);

            return true;
        }

        public async Task<bool> UpdateUser(User value)
        {
            await tonClient.InitIfNeeded();

            var state = await tonClient.RawGetAccountState(value.Address);

            value.LastSync = state.SyncUtime;

            if (state.LastTransactionId.Lt == value.LastTxLt && state.LastTransactionId.Hash == value.LastTxHash)
            {
                return false;
            }

            if (string.IsNullOrEmpty(state.Data))
            {
                throw new InvalidOperationException("State 'data' is empty (contract not yet deployed?), nothing to update");
            }

            value.LastTxLt = state.LastTransactionId.Lt;
            value.LastTxHash = state.LastTransactionId.Hash;

            var smc = await tonClient.SmcLoad(value.Address);
            var data = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_user_data"));
            await tonClient.SmcForget(smc.Id);

            if (data.ExitCode != 0)
            {
                throw new NonZeroExitCodeException(data.ExitCode, value.Address, "get_user_data");
            }

            // Method returns:
            // (int, int, slice, slice, int, cell) get_user_data()
            // (storage::init?, storage::index, storage::master_address, storage::user_address, storage::revoked_at, storage::content)

            var index = data.Stack[1].ToLong();
            if (index != value.Index)
            {
                throw new ApplicationException($"User index mismatch: {index} got, {value.Index} expected for {value.Address}.");
            }

            var userAddress = data.Stack[3].ToBoc().RootCells[0].BeginRead().LoadAddressIntStd(false);
            var revokedAt = data.Stack[4].ToInt();
            var content = data.Stack[5].ToBoc();

            value.UserAddress = userAddress;
            value.RevokedAt = revokedAt;

            FillUserContent(value, content.RootCells[0]);

            return true;
        }

        public async Task<(bool Updated, List<OrderResponse>? Responses)> UpdateOrder(Order value)
        {
            await tonClient.InitIfNeeded();

            var state = await tonClient.RawGetAccountState(value.Address);

            value.LastSync = state.SyncUtime;

            if (state.LastTransactionId.Lt == value.LastTxLt && state.LastTransactionId.Hash == value.LastTxHash)
            {
                return (false, null);
            }

            if (string.IsNullOrEmpty(state.Data))
            {
                throw new InvalidOperationException("State 'data' is empty (contract not yet deployed?), nothing to update");
            }

            value.LastTxLt = state.LastTransactionId.Lt;
            value.LastTxHash = state.LastTransactionId.Hash;

            // https://github.com/the-real-some-dao/a-careers-smc/blob/main/contracts/order.fc#L37
            var slice = Boc.ParseFromBase64(state.Data).RootCells[0].BeginRead();

            var index = (long)slice.LoadULong(64);
            if (index != value.Index)
            {
                throw new ApplicationException($"Order index mismatch: {index} got, {value.Index} expected for {value.Address}.");
            }

            _ = slice.LoadAddressIntStd(); // master_address

            var addresses = slice.LoadRef().BeginRead();
            value.CustomerAddress = addresses.LoadAddressIntStd(false);
            value.FreelancerAddress = addresses.TryLoadAddressIntStd(false);

            var content = slice.LoadDict();
            FillOrderContent(value, content);

            // "strange" price reading because value can be more than TON supply (and long type range) due to user error.
            value.Price = (decimal)slice.LoadCoinsToBigInt() / TonUtils.Coins.ToNano(1);

            value.Deadline = DateTimeOffset.FromUnixTimeSeconds(slice.LoadUInt(32));
            value.TimeForCheck = (int)slice.LoadUInt(32);
            _ = slice.LoadUInt(8); // protocol_fee_numerator
            _ = slice.LoadUInt(8); // protocol_fee_denominator
            var responsesDict = slice.TryLoadDict(); // responses
            value.Status = (int)slice.LoadUInt(8);
            _ = slice.LoadULong(64); // admin_count
            value.ResponsesCount = (int)slice.LoadUInt(8);
            value.CompletedAt = DateTimeOffset.FromUnixTimeSeconds(slice.LoadUInt(32));
            var arbitration = slice.LoadRef().BeginRead();
            arbitration.SkipBits(64); // admin_voted_count
            value.ArbitrationFreelancerPart = (int)arbitration.LoadUInt(8);

            var responses = new List<OrderResponse>();

            if (responsesDict != null)
            {
                var dict = responsesDict.ParseDictRef(267, s => s.LoadAddressIntStd(false));
                foreach (var item in dict)
                {
                    try
                    {
                        var vals = item.Value.ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)), x => x, StringComparer.Ordinal);

                        var fromFreelancer = StringComparer.Ordinal.Equals(item.Key, value.FreelancerAddress);

                        if (fromFreelancer)
                        {
                            value.Result = vals.TryGetValue(PropResult, out var s) ? s.LoadStringSnake(true) : null;
                        }

                        if (vals.TryGetValue(PropPrice, out var ps)
                            && vals.TryGetValue(PropDeadline, out var ds)
                            && vals.TryGetValue(PropText, out var ts))
                        {
                            var resp = new OrderResponse() { OrderId = value.Id, FreelancerAddress = item.Key };
                            responses.Add(resp);
                            // parse cells AFTER adding to list, so for wrong data we will have default "INVALID CELL CONTENT" text
                            resp.Price = (decimal)ps.LoadCoinsToBigInt() / TonUtils.Coins.ToNano(1);
                            resp.Deadline = DateTimeOffset.FromUnixTimeSeconds(ds.LoadInt(32));
                            resp.Text = ts.LoadStringSnake(true) ?? "- EMPTY -";

                            if (value.Status == Order.status_waiting_freelancer && fromFreelancer)
                            {
                                value.Price = resp.Price;
                                value.Deadline = resp.Deadline;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to parse response for order #{Index} {Address} from {User}", value.Index, value.Address, item.Key);
                    }
                }
            }

            return (true, responses);
        }

        public async IAsyncEnumerable<OrderActivity> GetOrderActivities(Order order, long endLt)
        {
            var start = new TransactionId() { Lt = order.LastTxLt, Hash = order.LastTxHash! };
            await foreach (var tx in EnumerateTransactions(order.Address, start, endLt))
            {
                var txboc = Boc.ParseFromBase64(tx.Data);
                var btx = new TonLibDotNet.BlocksTlb.Transaction(txboc.RootCells[0].BeginRead());
                var successful = btx.Description is TonLibDotNet.BlocksTlb.TransactionDescr.Ord to && !to.Aborted;

                if (!successful)
                {
                    logger.LogDebug("Tx for Order {Address} ignored: not successful at {Time} ({Lt}:{Hash})", order.Address, tx.Utime, tx.TransactionId.Lt, tx.TransactionId.Hash);
                    continue;
                }

                if (tx.InMsg == null)
                {
                    logger.LogDebug("Tx for Order {Address} ignored: in_msg is empty at {Time} ({Lt}:{Hash})", order.Address, tx.Utime, tx.TransactionId.Lt, tx.TransactionId.Hash);
                    continue;
                }

                if (tx.InMsg.MsgData is not TonLibDotNet.Types.Msg.DataRaw data)
                {
                    logger.LogDebug("Tx for Order {Address} ignored: in_msg.data is not raw at {Time} ({Lt}/{Hash})", order.Address, tx.Utime, tx.TransactionId.Lt, tx.TransactionId.Hash);
                    continue;
                }

                if (string.IsNullOrEmpty(data.Body))
                {
                    logger.LogDebug("Tx for Order {Address} ignored: in_msg.body is empty at {Time} ({Lt}/{Hash})", order.Address, tx.Utime, tx.TransactionId.Lt, tx.TransactionId.Hash);
                    continue;
                }

                var boc = Boc.ParseFromBase64(data.Body);
                var slice = boc.RootCells[0].BeginRead();

                if (slice.Length < 32)
                {
                    logger.LogDebug("Tx for Order {Address} ignored: in_msg.body is less than 32 bits at {Time} ({Lt}/{Hash})", order.Address, tx.Utime, tx.TransactionId.Lt, tx.TransactionId.Hash);
                    continue;
                }

                var op = slice.LoadInt(32);

                var activity = new OrderActivity
                {
                    OrderId = order.Id,
                    TxLt = tx.TransactionId.Lt,
                    TxHash = tx.TransactionId.Hash,
                    Timestamp = tx.Utime,
                    OpCode = op,
                    SenderAddress = TonUtils.Address.SetBounceable(tx.InMsg.Source.Value, false),
                    Amount = TonUtils.Coins.FromNano(tx.InMsg.Value),
                };

                yield return activity;
            }
        }

        public async IAsyncEnumerable<Transaction> EnumerateTransactions(string address, TransactionId start, long endLt)
        {
            await tonClient.InitIfNeeded();

            while (!start.IsEmpty())
            {
                var res = await tonClient.RawGetTransactions(address, start);
                if (res.TransactionsList.Count == 0)
                {
                    yield break;
                }

                foreach (var tx in res.TransactionsList)
                {
                    if (tx.TransactionId.Lt == endLt)
                    {
                        yield break;
                    }

                    yield return tx;
                }

                if (res.PreviousTransactionId.Lt == endLt)
                {
                    yield break;
                }
                else
                {
                    start = res.PreviousTransactionId;
                }
            }
        }

        public async Task<(DateTimeOffset syncTime, TransactionId lastTx, string stateHash, long nextAdminIndex, long nextUserIndex, long nextOrderIndex, List<Category>? categories, List<Language>? languages)> ParseMasterData(string address)
        {
            await tonClient.InitIfNeeded();

            var state = await tonClient.RawGetAccountState(address);

            var dataHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Convert.FromBase64String(state.Data)));

            var slice = Boc.ParseFromBase64(state.Data).RootCells[0].BeginRead();

            slice.SkipRef(); // contract codes

            var indexes = slice.LoadRef().BeginRead();
            var nextOrder = indexes.LoadLong();
            var nextUser = indexes.LoadLong();
            var nextAdmin = indexes.LoadLong();

            var categories = slice.TryLoadDict()?
                .ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)).ToLowerInvariant(), x => x)
                .Select(x =>
                {
                    var active = x.Value.LoadBit();
                    var name = x.Value.LoadRef().BeginRead().LoadStringSnake(true);
                    return new Category
                    {
                        Hash = x.Key,
                        Name = name ?? "???",
                        IsActive = active,
                    };
                })
                .ToList();

            // load languages as TryLoadRef instead of TryLoadDict, because:
            //   there are some fields that we have not skipped (order_fee_numerator, order_fee_denominator, user_creation_fee, order_creation_fee)
            //   and there are no other refs left
            var languages = slice.TryLoadRef()?
                .ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)).ToLowerInvariant(), x => x.LoadStringSnake(true))
                .Select(x => new Language() { Hash = x.Key, Name = x.Value ?? "???" })
                .ToList();

            return (state.SyncUtime, state.LastTransactionId, dataHash, nextAdmin, nextUser, nextOrder, categories, languages);
        }

        public IAsyncEnumerable<(long index, string address)> EnumerateAdminAddresses(string masterAddress, long fromIndex, long toIndex)
        {
            return EnumerateChildAddresses(masterAddress, "get_admin_address", fromIndex, toIndex);
        }

        public IAsyncEnumerable<(long index, string address)> EnumerateUserAddresses(string masterAddress, long fromIndex, long toIndex)
        {
            return EnumerateChildAddresses(masterAddress, "get_user_address", fromIndex, toIndex);
        }

        public IAsyncEnumerable<(long index, string address)> EnumerateOrderAddresses(string masterAddress, long fromIndex, long toIndex)
        {
            return EnumerateChildAddresses(masterAddress, "get_order_address", fromIndex, toIndex);
        }

        private async IAsyncEnumerable<(long index, string address)> EnumerateChildAddresses(string masterAddress, string methodName, long fromIndex, long toIndex)
        {
            await tonClient.InitIfNeeded();

            var smc = await tonClient.SmcLoad(masterAddress);

            while (fromIndex < toIndex)
            {
                var args = new List<TonLibDotNet.Types.Tvm.StackEntry>()
                {
                    new TonLibDotNet.Types.Tvm.StackEntryNumber(new TonLibDotNet.Types.Tvm.NumberDecimal(fromIndex)),
                };

                var resp = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName(methodName), args);

                var adr = resp.Stack[0].ToBoc().RootCells[0].BeginRead().LoadAddressIntStd(true);
                yield return (fromIndex, adr);

                fromIndex++;
            }

            await tonClient.SmcForget(smc.Id);
        }
    }
}
