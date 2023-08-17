using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public interface ISearchService
    {
        List<NftItem> Find(string text);
    }
}
