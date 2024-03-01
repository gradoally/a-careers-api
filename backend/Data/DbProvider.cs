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
            MainDb.CreateTable<Category>();
            MainDb.CreateTable<Language>();
            MainDb.CreateTable<Translation>();

            UpdateDb(MainDb);

            logger.LogInformation("Connected to {FilePath}", MainDb.DatabasePath);
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

            var ver = connection.Find<Settings>(Settings.KEY_DB_VERSION)?.IntValue ?? 0;

            if (ver == 0)
            {
                ver = minVersion;
                connection.Insert(new Settings(Settings.KEY_DB_VERSION, ver));
            }

            if (ver < minVersion)
            {
                throw new InvalidOperationException($"Too old version: {ver} (supported minumum: {minVersion})");
            }

            ////if (ver == 1)
            ////{
            ////    logger.LogInformation("Performing upgrade from version {Version}...", ver);

            ////    await connection.InsertOrReplace(...);

            ////    ver = 2;
            ////    await connection.InsertOrReplace(new Settings(Settings.KEY_DB_VERSION, ver));
            ////    logger.LogInformation("DB version updated to {Version}", ver);
            ////}
        }
    }
}
