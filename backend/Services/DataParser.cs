using SomeDAO.Backend.Data;
using TonLibDotNet;
using TonLibDotNet.Cells;
using TonLibDotNet.Types.Smc;

namespace SomeDAO.Backend.Services
{
    public class DataParser(ITonClient tonClient)
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
        private static readonly string PropPrice = GetSHA256OfStringAsHex("price");
        private static readonly string PropDeadline = GetSHA256OfStringAsHex("deadline");
        private static readonly string PropTechnicalTask = GetSHA256OfStringAsHex("technical_task");
        private static readonly string PropLanguage = GetSHA256OfStringAsHex("language");

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

            if (dict.TryGetValue(PropCategory, out var s))
            {
                value.Category = Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant();
            }

            if (dict.TryGetValue(PropCanApproveUser, out s))
            {
                value.CanApproveUser = s.LoadBit();
            }

            if (dict.TryGetValue(PropCanRevokeUser, out s))
            {
                value.CanRevokeUser = s.LoadBit();
            }

            if (dict.TryGetValue(PropNickname, out s))
            {
                value.Nickname = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropAbout, out s))
            {
                value.About = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropWebsite, out s))
            {
                value.Website = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropPortfolio, out s))
            {
                value.Portfolio = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropResume, out s))
            {
                value.Resume = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropSpecialization, out s))
            {
                value.Specialization = s.LoadStringSnake(true) ?? string.Empty;
            }
        }

        public static void FillUserContent(IUserContent value, Cell fromDict)
        {
            var dict = fromDict.ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)), x => x, StringComparer.Ordinal);
            if (dict == null)
            {
                return;
            }

            if (dict.TryGetValue(PropIsUser, out var s))
            {
                value.IsUser = s.LoadBit();
            }

            if (dict.TryGetValue(PropIsFreelancer, out s))
            {
                value.IsFreelancer = s.LoadBit();
            }

            if (dict.TryGetValue(PropNickname, out s))
            {
                value.Nickname = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropTelegram, out s))
            {
                value.Telegram = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropAbout, out s))
            {
                value.About = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropWebsite, out s))
            {
                value.Website = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropPortfolio, out s))
            {
                value.Portfolio = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropResume, out s))
            {
                value.Resume = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropSpecialization, out s))
            {
                value.Specialization = s.LoadStringSnake(true) ?? string.Empty;
            }
        }

        public static void FillOrderContent(IOrderContent value, Cell fromDict)
        {
            var dict = fromDict.ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)), x => x, StringComparer.Ordinal);
            if (dict == null)
            {
                return;
            }

            if (dict.TryGetValue(PropCategory, out var s))
            {
                value.Category = Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant();
            }

            if (dict.TryGetValue(PropLanguage, out s))
            {
                value.Language = Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant();
            }

            if (dict.TryGetValue(PropName, out s))
            {
                value.Name = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropPrice, out s))
            {
                value.Price = TonUtils.Coins.FromNano(s.LoadCoins());
            }

            if (dict.TryGetValue(PropDeadline, out s))
            {
                var d = s.LoadUInt(32);
                value.Deadline = DateTimeOffset.FromUnixTimeSeconds(d);
            }

            if (dict.TryGetValue(PropDescription, out s))
            {
                value.Description = s.LoadStringSnake(true) ?? string.Empty;
            }

            if (dict.TryGetValue(PropTechnicalTask, out s))
            {
                value.TechnicalTask = s.LoadStringSnake(true) ?? string.Empty;
            }
        }

        public async Task<bool> UpdateAdmin(Admin value)
        {
            await tonClient.InitIfNeeded().ConfigureAwait(false);

            var state = await tonClient.RawGetAccountState(value.Address).ConfigureAwait(false);

            value.LastSync = state.SyncUtime;

            if (state.LastTransactionId.Lt == value.LastTxLt && state.LastTransactionId.Hash == value.LastTxHash)
            {
                return false;
            }

            value.LastTxLt = state.LastTransactionId.Lt;
            value.LastTxHash = state.LastTransactionId.Hash;

            var smc = await tonClient.SmcLoad(value.Address).ConfigureAwait(false);
            var data = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_admin_data")).ConfigureAwait(false);
            await tonClient.SmcForget(smc.Id).ConfigureAwait(false);

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
            await tonClient.InitIfNeeded().ConfigureAwait(false);

            var state = await tonClient.RawGetAccountState(value.Address).ConfigureAwait(false);

            value.LastSync = state.SyncUtime;

            if (state.LastTransactionId.Lt == value.LastTxLt && state.LastTransactionId.Hash == value.LastTxHash)
            {
                return false;
            }

            value.LastTxLt = state.LastTransactionId.Lt;
            value.LastTxHash = state.LastTransactionId.Hash;

            var smc = await tonClient.SmcLoad(value.Address).ConfigureAwait(false);
            var data = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_user_data")).ConfigureAwait(false);
            await tonClient.SmcForget(smc.Id).ConfigureAwait(false);

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
            value.RevokedAt = revokedAt == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(revokedAt);

            FillUserContent(value, content.RootCells[0]);

            return true;
        }

        public async Task<bool> UpdateOrder(Order value)
        {
            await tonClient.InitIfNeeded().ConfigureAwait(false);

            var state = await tonClient.RawGetAccountState(value.Address).ConfigureAwait(false);

            value.LastSync = state.SyncUtime;

            if (state.LastTransactionId.Lt == value.LastTxLt && state.LastTransactionId.Hash == value.LastTxHash)
            {
                return false;
            }

            value.LastTxLt = state.LastTransactionId.Lt;
            value.LastTxHash = state.LastTransactionId.Hash;

            var smc = await tonClient.SmcLoad(value.Address).ConfigureAwait(false);

            var data1 = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_order_data")).ConfigureAwait(false);
            if (data1.ExitCode != 0)
            {
                throw new NonZeroExitCodeException(data1.ExitCode, value.Address, "get_order_data");
            }

            var data2 = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_responses")).ConfigureAwait(false);
            if (data2.ExitCode != 0)
            {
                throw new NonZeroExitCodeException(data2.ExitCode, value.Address, "get_responses");
            }

            await tonClient.SmcForget(smc.Id).ConfigureAwait(false);

            // Method returns:
            // (int, int, slice, int, int, int, slice, slice, cell) get_order_data()
            // (storage::init?, storage::index, storage::master_address,
            //      storage::status, storage::price, storage::deadline, storage::customer_address,
            //      storage::freelancer_address, storage::content)

            // Second method returns:
            // (cell, int) get_responses()
            // (storage::responses, storage::responses_count)

            var index = data1.Stack[1].ToLong();

            if (index != value.Index)
            {
                throw new ApplicationException($"Order index mismatch: {index} got, {value.Index} expected for {value.Address}.");
            }

            var status = data1.Stack[3].ToInt();
            var price = TonUtils.Coins.FromNano(data1.Stack[4].ToLong());
            var deadline = data1.Stack[5].ToInt();
            var customerAddress = data1.Stack[6].ToBoc().RootCells[0].BeginRead().LoadAddressIntStd(false);
            var freelancerAddress = data1.Stack[7].ToBoc().RootCells[0].BeginRead().TryLoadAddressIntStd(false);
            var content = data1.Stack[8].ToBoc();

            var responsesCount = data2.Stack[1].ToInt();

            value.Status = (OrderStatus)status;
            value.CustomerAddress = customerAddress;
            value.FreelancerAddress = freelancerAddress;
            value.CreatedAt = DateTimeOffset.UtcNow.Truncate(TimeSpan.FromSeconds(1));
            value.ResponsesCount = responsesCount;

            FillOrderContent(value, content.RootCells[0]);

            // overwrite values from content
            value.Price = price;
            value.Deadline = DateTimeOffset.FromUnixTimeSeconds(deadline);

            return true;
        }
    }
}
