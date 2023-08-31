using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public interface ISearchService
    {
        const string OrderAsc = "asc";
        const string OrderDesc = "desc";

        int Count { get; }

        List<Order> Find(string? query, string? status, string? category, decimal? minAmount, decimal? maxAmount, string orderByField, string orderBySort);
    }
}
