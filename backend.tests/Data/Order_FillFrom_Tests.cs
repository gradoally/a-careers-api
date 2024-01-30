using SomeDAO.Backend.Services;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Data
{
    public class Order_FillFrom_Tests
    {
        [Fact]
        public void MustParseOrderContent()
        {
            // last cell of get_order_data() of https://tonviewer.com/EQAup_18ePpROCkOKYj6o3IBrElsq5osmgAj-_276gPWHNQi?section=method
            const string dataHex = "b5ee9c72010214010001af000201200102020158030402012007080141bf7585ad32b555c39c0b0e5916ff9db998fcd4b7501a30ea695fa10eecd07c6ef5050141bf5076a63e83d91145e68b6e4f5fcb2eaf6e62c546814cb90d774c50cdf1fcaad9060022746f6e3a2f2f73746f726167652e746f6e0011802c68af0bb1400008020120090a0201200d0e0141bf4546a6ffe1b79cfdd86bad3db874313dcde2fb05e6a74aa7f3552d9617c79d130b0141bf49de6097485440175faf1608dc10d35f2307edddcaa495d5abb9674b56bf1e7f0c002046697273742074657374206f7264657200405bcc40adf6e0a2a9c9317f9ac01481271b45ffdd5850d96b562752eee8167b850201200f100141bf5b659a76e993335ee1e16e0a9321e40e5b8dc21508f5edda0b2a9713296e61fd130141bf2411bde8deb43a9f3b9ccd56613e950a260be2cdf23def3b247deb1c69f34412110141bf3f22bace60a38c7133e3fb154f1dad973e46d7b22cac03ce5de22e7d62ef603a12003244657372697074696f6e206f66206669727374206f72646572000865ba6e8000402658a7ac4df496ad72af39e532297cd89038ceb29d1c630e105bc4ebb24a3ba9";

            var obj = new Order();

            DataParser.FillOrderContent(obj, Boc.ParseFromBytes(Convert.FromHexString(dataHex)).RootCells[0]);

            Assert.Equal(
                "0x2658a7ac4df496ad72af39e532297cd89038ceb29d1c630e105bc4ebb24a3ba9",
                obj.Category);

            Assert.Equal(
                "0x5bcc40adf6e0a2a9c9317f9ac01481271b45ffdd5850d96b562752eee8167b85",
                obj.Language);

            Assert.Equal(
                "First test order",
                obj.Name);

            Assert.Equal(
                200000000,
                obj.Price);

            Assert.Equal(
                new DateTimeOffset(2024, 1, 31, 16, 0, 0, TimeSpan.Zero),
                obj.Deadline);

            Assert.Equal(
                "Desription of first order",
                obj.Description);

            Assert.Equal(
                "ton://storage.ton",
                obj.TechnicalTask);
        }
    }
}
