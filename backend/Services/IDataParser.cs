using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public interface IDataParser
    {
        Task<NftItem?> GetNftItem(string address);
    }
}
