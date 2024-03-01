using SQLite;

namespace SomeDAO.Backend.Data
{
    public interface IDbProvider
    {
        SQLiteConnection MainDb { get; }

        void Migrate();

        Task Reconnect();
    }
}
