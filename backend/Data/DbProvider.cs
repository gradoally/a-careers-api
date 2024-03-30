using Microsoft.Extensions.Options;
using SQLite;

namespace SomeDAO.Backend.Data
{
    public class DbProvider : IDbProvider, IDisposable
    {
        private readonly BackendOptions options;
        private readonly ILogger logger;

        private bool disposedValue;

        public DbProvider(IOptions<BackendOptions> options, ILogger<DbProvider> logger)
        {
            if (options?.Value == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.options = options.Value;
            MainDb = GetMainDb(this.options);
        }

        public SQLiteConnection MainDb { get; private set; }

        public void Migrate()
        {
            MainDb.CreateTable<Settings>();
            MainDb.CreateTable<Admin>();
            MainDb.CreateTable<User>();
            MainDb.CreateTable<Order>();
            MainDb.CreateTable<SyncQueueItem>();
            MainDb.CreateTable<OrderActivity>();
            MainDb.CreateTable<OrderResponse>();
            MainDb.CreateTable<Category>();
            MainDb.CreateTable<Language>();
            MainDb.CreateTable<Translation>();
            MainDb.CreateTable<NotificationQueueItem>();

            UpdateDb(MainDb);

            logger.LogInformation("Using {FilePath}", MainDb.DatabasePath);
        }

        public async Task Reconnect()
        {
            var olddb = MainDb;
            MainDb = GetMainDb(options);
            logger.LogDebug("Reconnected to DB");
            await Task.Delay(TimeSpan.FromSeconds(5));
            olddb.Close();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MainDb.Close();
                }

                disposedValue = true;
            }
        }

        protected SQLiteConnection GetMainDb(BackendOptions options)
        {
            var file = Path.GetFullPath(options.DatabaseFile);
            var conn = new SQLiteConnection(file);
            conn.ExecuteScalar<string>("PRAGMA journal_mode=WAL");
            return conn;
        }

        protected void UpdateDb(SQLiteConnection connection)
        {
            const int minVersion = 1;
            const int lastVersion = 3;

            var ver = connection.Find<Settings>(Settings.KEY_DB_VERSION)?.IntValue ?? 0;

            if (ver == 0)
            {
                ver = lastVersion;
                connection.Insert(new Settings(Settings.KEY_DB_VERSION, ver));
            }

            if (ver < minVersion)
            {
                throw new InvalidOperationException($"Too old version: {ver} (supported minumum: {minVersion})");
            }

            if (ver == 1)
            {
                logger.LogInformation("Performing upgrade from version {Version}...", ver);

                connection.Execute("ALTER TABLE OrderActivity DROP COLUMN SenderRole;");

                ver++;
                connection.InsertOrReplace(new Settings(Settings.KEY_DB_VERSION, ver));
                logger.LogInformation("DB version updated to {Version}", ver);
            }

            if (ver == 2)
            {
                logger.LogInformation("Performing upgrade from version {Version}...", ver);

                connection.Execute("UPDATE [Order] SET LastTxLt = 0, LastSync = 0;");

                ver++;
                connection.InsertOrReplace(new Settings(Settings.KEY_DB_VERSION, ver));
                logger.LogInformation("DB version updated to {Version}", ver);
            }

            if (ver != lastVersion)
            {
                throw new ApplicationException($"Failed to update DB: actual version {ver} does not equal to expected {lastVersion}");
            }
        }
    }
}
