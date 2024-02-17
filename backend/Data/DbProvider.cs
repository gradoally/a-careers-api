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
            MainDb = GetMainDb(this.options).GetAwaiter().GetResult();
        }

        public SQLiteAsyncConnection MainDb { get; private set; }

        public async Task Reconnect()
        {
            var olddb = MainDb;
            MainDb = await GetMainDb(options).ConfigureAwait(false);
            logger.LogDebug("Reconnected to DB");
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            await olddb.CloseAsync().ConfigureAwait(false);
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
                    MainDb.CloseAsync().GetAwaiter().GetResult();
                }

                disposedValue = true;
            }
        }

        protected async Task<SQLiteAsyncConnection> GetMainDb(BackendOptions options)
        {
            var file = Path.GetFullPath(options.DatabaseFile);
            var conn = new SQLiteAsyncConnection(file);
            logger.LogInformation("Connected to {FilePath}", file);

            await conn.ExecuteScalarAsync<string>("PRAGMA journal_mode=WAL");

            await conn.CreateTableAsync<Settings>().ConfigureAwait(false);
            await conn.CreateTableAsync<Admin>().ConfigureAwait(false);
            await conn.CreateTableAsync<User>().ConfigureAwait(false);
            await conn.CreateTableAsync<Order>().ConfigureAwait(false);
            await conn.CreateTableAsync<SyncQueueItem>().ConfigureAwait(false);
            await conn.CreateTableAsync<OrderActivity>().ConfigureAwait(false);
            await conn.CreateTableAsync<Category>().ConfigureAwait(false);
            await conn.CreateTableAsync<Language>().ConfigureAwait(false);
            await conn.CreateTableAsync<Translation>().ConfigureAwait(false);

            await UpdateDb(conn).ConfigureAwait(false);

            return conn;
        }

        protected async Task UpdateDb(SQLiteAsyncConnection connection)
        {
            const int minVersion = 1;

            var ver = (await connection.FindAsync<Settings>(Settings.KEY_DB_VERSION).ConfigureAwait(false))?.IntValue ?? 0;

            if (ver == 0)
            {
                ver = minVersion;
                await connection.InsertAsync(new Settings(Settings.KEY_DB_VERSION, ver)).ConfigureAwait(false);
            }

            if (ver < minVersion)
            {
                throw new InvalidOperationException($"Too old version: {ver} (supported minumum: {minVersion})");
            }

            ////if (ver == 1)
            ////{
            ////    logger.LogInformation("Performing upgrade from version {Version}...", ver);

            ////    await connection.InsertOrReplaceAsync(...).ConfigureAwait(false);

            ////    ver = 2;
            ////    await connection.InsertOrReplaceAsync(new Settings(Settings.KEY_DB_VERSION, ver)).ConfigureAwait(false);
            ////    logger.LogInformation("DB version updated to {Version}", ver);
            ////}
        }
    }
}
