using SomeDAO.Backend.Services;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Data
{
    public class Admin_FillFrom_Tests
    {
        [Fact]
        public void MustParseAdminContent()
        {
            // last cell of get_admin_data() of https://tonviewer.com/EQDMMZ8op6c6G9tMNVgLs0-DkijEztCmTkPGd18RhYnjt76F?section=method
            const string dataHex = "b5ee9c72010216010001b50002012001020201200304020120090a020378e0050602027607080141be6c98ff522baf1a23fcad6f8c5f308a1adc1619d070796a294b6f3ab8e9e8a970140141be4d51c17093db360c98b44bc3dcab25e84fc7d73a4a1c840d39c8c28fa01d1a90110141bea46a0702997b4b8b348d91465cdd8e9476b14380de1fb65616d146da5a09c678110141be87a8f398395dde8e524d9f983784bd8441c5cfe4307b5a079be5412ee65c31481102016a0b0c02012012130201200d0e0141bec1d189906c90dc445247d2f6e85a5ad14cc26f374b5fbdc563e94dd03657b0b4110141be8fb3f1159c9dfba179a5fe91f4214f26be112a36e24c2c468b10bffb11292fc80f0141be8262e1c9bcbc1721eb3fe13558154460a2b2a2d307daa32532478526ce6ccb18100016736f6d655f77616c6c65740024416c6661204d6174657220466f756e64657200000141bf46be116c613d10dfdc990a3b95c8b7c0122324c4828c8e1a9031127e8a9891a7140141bf5b659a76e993335ee1e16e0a9321e40e5b8dc21508f5edda0b2a9713296e61fd150001c000405ef5ef0364b6939c4ca61f34b393f7b368d1be8619647aaf83d5b395919ab629";

            var obj = new Admin();

            DataParser.FillAdminContent(obj, Boc.ParseFromBytes(Convert.FromHexString(dataHex)).RootCells[0]);

            Assert.Equal(
                "0x5ef5ef0364b6939c4ca61f34b393f7b368d1be8619647aaf83d5b395919ab629",
                obj.Category);

            Assert.True(obj.CanApproveUser);

            Assert.True(obj.CanRevokeUser);

            Assert.Equal(
                "some_wallet",
                obj.Nickname);

            Assert.Equal(
                "Alfa Mater Founder",
                obj.About);

            Assert.Equal(
                string.Empty,
                obj.Website);

            Assert.Equal(
                string.Empty,
                obj.Portfolio);

            Assert.Equal(
                string.Empty,
                obj.Resume);

            Assert.Equal(
                string.Empty,
                obj.Specialization);
        }

    }
}
