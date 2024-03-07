using System.Diagnostics;
using Microsoft.Extensions.Options;
using RecurrentTasks;

namespace SomeDAO.Backend.Services
{
    public class IndexerControlTask : IRunnable, IDisposable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

        private readonly ILogger logger;
        private readonly TimeSpan indexerRestartInterval;

        private Process? indexer;
        private DateTimeOffset indexerRestartTime = DateTimeOffset.MaxValue;
        private bool disposed;

        public IndexerControlTask(ILogger<IndexerControlTask> logger, IOptions<BackendOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.indexerRestartInterval = options.Value.IndexerSubprocessRestartInterval;
        }

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (indexer != null)
            {
                indexer.Refresh();

                if (indexer.HasExited)
                {
                    logger.LogDebug("Indexer process HasExited=true, disposing...");
                    indexer.Close();
                    indexer = null;
                }
                else if (DateTimeOffset.UtcNow > indexerRestartTime)
                {
                    logger.LogDebug("Indexer process running too long, killing...");
                    indexer.Kill();
                }
            }

            indexer ??= StartIndexer();

            return Task.CompletedTask;
        }

        protected Process StartIndexer()
        {
            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                WorkingDirectory = Environment.CurrentDirectory,
                ArgumentList =
                    {
                        Program.StartAsIndexerArg,
                    },
            };

            logger.LogDebug("WorkingDir: {Dir}", psi.WorkingDirectory);
            logger.LogDebug("Executable: {Exe}", psi.FileName);

            indexerRestartTime = indexerRestartInterval.Ticks > 0
                ? DateTimeOffset.UtcNow.Add(indexerRestartInterval)
                : DateTimeOffset.MaxValue;

            var p = Process.Start(psi)!;

            logger.LogInformation("Process started: PID={PID}", p.Id);

            return p;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    indexer?.Kill();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
