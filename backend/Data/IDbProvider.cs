using SQLite;

namespace SomeDAO.Backend.Data
{
    public interface IDbProvider
    {
        SQLiteAsyncConnection MainDb { get; }

        Task Reconnect();
    }
}
