using SomeDAO.Backend.Services;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Data
{
    public class User_FillFrom_Tests
    {
        [Fact]
        public void MustParseUserContent()
        {
            // last cell of get_user_data() of https://tonviewer.com/EQCk_PlzthTcQWbOXe_bhbWs6nsgwAWOdwF-mwHTF5vBYO0s?section=method
            const string dataHex = "b5ee9c72010216010001b000020120010202012003040201200c0d02012005060202760a0b02012007080141bf7e808c522b47cc04d49af2024f72bbda9b0e1ed5631b35e5f9788a82d04aa46f090141bf39aa382e127b66c1931689787b9564bd09f8fae749439081a7391851f403a352140141bf102851c0c3961335284e3d33d70756be1d0a830b6e51f4a04ba4657fd2c6dd3615001840736f6d655f77616c6c65740141bea46a0702997b4b8b348d91465cdd8e9476b14380de1fb65616d146da5a09c678140141be87a8f398395dde8e524d9f983784bd8441c5cfe4307b5a079be5412ee65c31481402016a0e0f0142bf9ec8aefcdeec684046f2714ca58167d25adbd81dca3a47d34fcb7d6f5cdb012f1502012010110141bec1d189906c90dc445247d2f6e85a5ad14cc26f374b5fbdc563e94dd03657b0b4140141be8fb3f1159c9dfba179a5fe91f4214f26be112a36e24c2c468b10bffb11292fc8120141be8262e1c9bcbc1721eb3fe13558154460a2b2a2d307daa32532478526ce6ccb1813001466697273745f757365720046466972737420746573742075736572206f6e20467265656c616e63652043656e74657200000001c0";

            IUserContent obj = new User();

            DataParser.FillUserContent(obj, Boc.ParseFromHex(dataHex).RootCells[0]);

            Assert.True(obj.IsUser);

            Assert.True(obj.IsFreelancer);

            Assert.Equal(
                "first_user",
                obj.Nickname);

            Assert.Equal(
                "@some_wallet",
                obj.Telegram);

            Assert.Equal(
                "First test user on Freelance Center",
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
