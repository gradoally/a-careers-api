using SQLite;

namespace SomeDAO.Backend.Services
{
    public interface IDbProvider
    {
        SQLiteAsyncConnection MainDb { get; }
    }
}
