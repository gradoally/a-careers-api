using SQLite;

namespace SomeDAO.Backend.Data
{
    public interface IDbProvider
    {
        SQLiteConnection MainDb { get; }

        Task Reconnect();
    }
}
