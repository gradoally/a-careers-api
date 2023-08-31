using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public interface ISearchService
    {
        int Count { get; }

        List<Order> Find(string text);
    }
}
