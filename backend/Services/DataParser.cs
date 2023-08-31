using System.Globalization;
using SomeDAO.Backend.Data;
using TonLibDotNet;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Services
{
    public class DataParser : IDataParser
    {
        private readonly ILogger logger;
        private readonly ITonClient tonClient;

        public const string PropNameImage = "image";
        public const string PropNameName = "name";
        public const string PropNameDescription = "description";
        public const string PropNameStatus = "status";
        public const string PropNameAmount = "amount";
        public const string PropNameTechAssignment = "technical_assigment";
        public const string PropNameStartUnixTime = "starting_unix_time";
        public const string PropNameEndUnixTime = "ending_unix_time";
        public const string PropNameCreateUnixTime = "creation_unix_time";
        public const string PropNameCategory = "category";
        public const string PropNameCustomer = "customer_addr";

        protected static readonly string PropImage = EncodePropertyName(PropNameImage);
        protected static readonly string PropName = EncodePropertyName(PropNameName);
        protected static readonly string PropDescription = EncodePropertyName(PropNameDescription);
        protected static readonly string PropStatus = EncodePropertyName(PropNameStatus);
        protected static readonly string PropAmount = EncodePropertyName(PropNameAmount);
        protected static readonly string PropTechAssignment = EncodePropertyName(PropNameTechAssignment);
        protected static readonly string PropStartUnixTime = EncodePropertyName(PropNameStartUnixTime);
        protected static readonly string PropEndUnixTime = EncodePropertyName(PropNameEndUnixTime);
        protected static readonly string PropCreateUnixTime = EncodePropertyName(PropNameCreateUnixTime);
        protected static readonly string PropCategory = EncodePropertyName(PropNameCategory);
        protected static readonly string PropCustomer = EncodePropertyName(PropNameCustomer);

        protected static string EncodePropertyName(string name)
        {
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(name)));
        }

        public DataParser(ILogger<DataParser> logger, ITonClient tonClient)
        {
            this.logger = logger;
            this.tonClient = tonClient;
        }

        public async Task<Order> GetNftItem(string address)
        {
            await tonClient.InitIfNeeded().ConfigureAwait(false);

            var state = await tonClient.RawGetAccountState(address).ConfigureAwait(false);

            var data = await TonRecipes.NFTs.GetNftData(tonClient, address);
            var content = await TonRecipes.NFTs.GetNftContent(tonClient, data.collectionAddress, data.index, data.individualContent);

            var info2 = ParseNftContent(content);

            var item = new Order
            {
                Index = (long)data.index,
                Address = address,
                OwnerAddress = data.ownerAddress,

                Image = info2.image,
                Status = info2.status,
                Name = info2.name,
                Amount = info2.amount,
                Description = info2.description,
                Assignment = info2.assignment,
                Category = info2.category,
                Customer = info2.customer,
                Created = DateTimeOffset.FromUnixTimeSeconds(info2.creation),
                Starting = DateTimeOffset.FromUnixTimeSeconds(info2.starting),
                Ending = DateTimeOffset.FromUnixTimeSeconds(info2.ending),

                LastUpdate = DateTimeOffset.UtcNow,
                LastTxHash = state.LastTransactionId.Hash,
                LastTxLt = state.LastTransactionId.Lt,
                UpdateNeeded = false,
            };

            return item;
        }

        public static (ulong index, string collectionAddress, string? ownerAddress, string? editorAddress) ParseNftData(string base64boc)
        {
            var boc = Boc.ParseFromBase64(base64boc);
            var cell = boc.RootCells[0].BeginRead();

            var index = cell.LoadULong();
            var collectionAddress = cell.LoadAddressIntStd();
            var ownerAddress = cell.TryLoadAddressIntStd();
            var editorAddress = cell.TryLoadAddressIntStd();

            return (index, collectionAddress, ownerAddress, editorAddress);
        }

        public static (
            string image, string status, string name,
            decimal amount, string description, string assignment,
            long starting, long ending, long creation,
            string category, string customer) ParseNftContent(Boc boc)
        {
            var slice = boc.RootCells[0].BeginRead();

            var prefix = slice.LoadByte();
            if (prefix != 0x00)
            {
                return (
                    string.Empty, string.Empty, string.Empty,
                    default, string.Empty, string.Empty,
                    default, default, default,
                    string.Empty, string.Empty);
            }

            var dic = slice.TryLoadAndParseDict(256, x => Convert.ToHexString(x.LoadBytes(32)), x => x);

            var image = string.Empty;
            var status = string.Empty;
            var name = string.Empty;
            var amount = 0M;
            var description = string.Empty;
            var assignment = string.Empty;
            var start = 0L;
            var end = 0L;
            var created = 0L;
            var category = string.Empty;
            var customer = string.Empty;

            Slice? s = null;

            if (dic != null)
            {
                if (dic.TryGetValue(PropImage, out s))
                {
                    image = s.LoadStringSnake();
                }

                if (dic.TryGetValue(PropStatus, out s))
                {
                    status = s.LoadStringSnake();
                }

                if (dic.TryGetValue(PropName, out s))
                {
                    name = s.LoadStringSnake();
                }

                if (dic.TryGetValue(PropAmount, out s))
                {
                    amount = decimal.Parse(s.LoadStringSnake(), CultureInfo.InvariantCulture);
                }

                if (dic.TryGetValue(PropDescription, out s))
                {
                    description = s.LoadStringSnake();
                }

                if (dic.TryGetValue(PropTechAssignment, out s))
                {
                    assignment = s.LoadStringSnake();
                }

                if (dic.TryGetValue(PropStartUnixTime, out s))
                {
                    start = long.Parse(s.LoadStringSnake(), CultureInfo.InvariantCulture);
                }

                if (dic.TryGetValue(PropEndUnixTime, out s))
                {
                    end = long.Parse(s.LoadStringSnake(), CultureInfo.InvariantCulture);
                }

                if (dic.TryGetValue(PropCreateUnixTime, out s))
                {
                    created = long.Parse(s.LoadStringSnake(), CultureInfo.InvariantCulture);
                }

                if (dic.TryGetValue(PropCategory, out s))
                {
                    category = s.LoadStringSnake();
                }

                if (dic.TryGetValue(PropCustomer, out s))
                {
                    customer = s.LoadStringSnake();
                }
            }

            return (
                image, status, name,
                amount, description, assignment,
                start, end, created,
                category, customer);
        }
    }
}
