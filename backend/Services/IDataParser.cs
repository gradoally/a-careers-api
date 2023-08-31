using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public interface IDataParser
    {
        Task<Order> GetNftItem(string address);
    }
}
