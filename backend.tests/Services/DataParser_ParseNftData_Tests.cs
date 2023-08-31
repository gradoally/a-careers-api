using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Services
{
    public class DataParser_ParseNftData_Tests
    {
        [Fact]
        public void MustParseNftData()
        {
            // Raw data of https://tonscan.org/address/EQDt2B8cBXBy3YMJ_orG__bkfGVG_41oHnUM_QV640FgFsnP
            const string dataBase64 = "te6cckECHwEAAzUAAdkAAAAAAAAAAIAFoG3q+vObUWIUQQWuxTEQKe4tNHN5FkiMyXd2mtaL2dABwc+s/m4HZMHlP9hD2jDiEJwQPtQRRDYeg4EUXFAXt54AODn1n83A7Jg8p/sIe0YcQhOCB9qCKIbD0HAii4oC9vPAAQEBwAICASADBAIBIAUGAgEgCgsCASAHCAFCv6EF1sx2r0ADJelNWIzlEb5b/btztDfcUeykORfXpD49EQFBv054LGmJLZtsk6NfwUYld2lvwug2TiqFyUhth3EEli7DCQFBv3SvYLdHU1PrQTlGJPA8Pr1GLie9JTneTBfEWtiVGDfpHQAOAEFjdGl2ZQIBIAwNAgEgEhMCAUgODwFBv295loYhEUAAGGb1u5ezxcDWWwzOofjYAWUi/4/UkZnPEQFBvtUam/+G3nP3Ya609uHQxPc3i+wXmp0qn81UtlhfHnRMEAFBvuYeUT5vZbeQ/0epBuayHL5f6F8l6HVSrclIy0R6vE8sHQAWAFNvbWUgT3JkZXIAtgBodHRwczovL3doYWxlcy5pbmZ1cmEtaXBmcy5pby9pcGZzL1FtUTVRaXVMQkVtRGRRbWRXY0VFaDJyc1c1M0tXYWhjNjN4bVBWQlVTcDR0ZUcvMzg4MC5wbmcCASAUFQFBv1tlmnbpkzNe4eFuCpMh5A5bjcIVCPXt2gsqlxMpbmH9HgIBIBYXAUG/GIFT7ah7Rr0ehhdYJ/XiPKBldqsxVAh7/Pn9Z2NxQSYdAUG+3g271sCu1j+apkP3gh5yxbUBHv1RyWYESchIgZjxw+wYAgEgGRoAYgBFUURXZlRWMFh0dVVyUllGOEJxT20xVTJ5cjNheFlscHZ4eG5HWHl4Mm53SXlwTTMBQb6QRvejetDqfO5zNVmE+lQomC+LN8j3vOyR96xxp80QSBsBQb6zjZXJxrHZ1RJcBNQaVN9Xcn70z7P1EWpgL+KyURXBOBwAYABTb21lIE9yZGVyIERlc3JpcHRpb24KCnRlc3Q6IDEKdGVzdDogMgp0ZXN0OiAzCgAKADEwMDAAFgAxNjkyODMxNjAwACoA0JHQu9C+0LrRh9C10LnQvSBUT04f5rlc";

            var parsed = DataParser.ParseNftData(dataBase64);

            Assert.Equal((ulong)0, parsed.index);
            Assert.Equal("EQAtA29X15zaixCiCC12KYiBT3Fpo5vIskRmS7u01rRezmHS", parsed.collectionAddress);
            Assert.Equal("EQBwc-s_m4HZMHlP9hD2jDiEJwQPtQRRDYeg4EUXFAXt50G9", parsed.ownerAddress);
            Assert.Equal("EQBwc-s_m4HZMHlP9hD2jDiEJwQPtQRRDYeg4EUXFAXt50G9", parsed.editorAddress);
        }

        [Fact]
        public void MustParseNftContent()
        {
            // get_nft_content of https://tonscan.org/address/EQDt2B8cBXBy3YMJ_orG__bkfGVG_41oHnUM_QV640FgFsnP
            const string dataBase64 = "te6cckECHgEAAsYAAQMAwAECASACAwIBIAQFAgEgCQoCASAGBwFCv6EF1sx2r0ADJelNWIzlEb5b/btztDfcUeykORfXpD49EAFBv054LGmJLZtsk6NfwUYld2lvwug2TiqFyUhth3EEli7DCAFBv3SvYLdHU1PrQTlGJPA8Pr1GLie9JTneTBfEWtiVGDfpHAAOAEFjdGl2ZQIBIAsMAgEgERICAUgNDgFBv295loYhEUAAGGb1u5ezxcDWWwzOofjYAWUi/4/UkZnPEAFBvtUam/+G3nP3Ya609uHQxPc3i+wXmp0qn81UtlhfHnRMDwFBvuYeUT5vZbeQ/0epBuayHL5f6F8l6HVSrclIy0R6vE8sHAAWAFNvbWUgT3JkZXIAtgBodHRwczovL3doYWxlcy5pbmZ1cmEtaXBmcy5pby9pcGZzL1FtUTVRaXVMQkVtRGRRbWRXY0VFaDJyc1c1M0tXYWhjNjN4bVBWQlVTcDR0ZUcvMzg4MC5wbmcCASATFAFBv1tlmnbpkzNe4eFuCpMh5A5bjcIVCPXt2gsqlxMpbmH9HQIBIBUWAUG/GIFT7ah7Rr0ehhdYJ/XiPKBldqsxVAh7/Pn9Z2NxQSYcAUG+3g271sCu1j+apkP3gh5yxbUBHv1RyWYESchIgZjxw+wXAgEgGBkAYgBFUURXZlRWMFh0dVVyUllGOEJxT20xVTJ5cjNheFlscHZ4eG5HWHl4Mm53SXlwTTMBQb6QRvejetDqfO5zNVmE+lQomC+LN8j3vOyR96xxp80QSBoBQb6zjZXJxrHZ1RJcBNQaVN9Xcn70z7P1EWpgL+KyURXBOBsAYABTb21lIE9yZGVyIERlc3JpcHRpb24KCnRlc3Q6IDEKdGVzdDogMgp0ZXN0OiAzCgAKADEwMDAAFgAxNjkyODMxNjAwACoA0JHQu9C+0LrRh9C10LnQvSBUT07odgef";

            var parsed = DataParser.ParseNftContent(Boc.ParseFromBase64(dataBase64));

            Assert.Equal(
                "https://whales.infura-ipfs.io/ipfs/QmQ5QiuLBEmDdQmdWcEEh2rsW53KWahc63xmPVBUSp4teG/3880.png",
                parsed.image);

            Assert.Equal(
                "Active",
                parsed.status);

            Assert.Equal(
                "Some Order",
                parsed.name);

            Assert.Equal(
                1000M,
                parsed.amount);

            Assert.Equal(
                "Some Order Desription\n\ntest: 1\ntest: 2\ntest: 3\n",
                parsed.description);

            Assert.Equal(
                "https://whales.infura-ipfs.io/ipfs/QmQ5QiuLBEmDdQmdWcEEh2rsW53KWahc63xmPVBUSp4teG/3880.png",
                parsed.assignment);

            Assert.Equal(
                1692831600,
                parsed.starting);

            Assert.Equal(
                1692831600,
                parsed.ending);

            Assert.Equal(
                1692831600,
                parsed.creation);

            Assert.Equal(
                "Блокчейн TON",
                parsed.category);

            Assert.Equal(
                "EQDWfTV0XtuUrRYF8BqOm1U2yr3axYlpvxxnGXyx2nwIypM3",
                parsed.customer);
        }
    }
}
