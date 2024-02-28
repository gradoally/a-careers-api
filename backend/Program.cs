namespace SomeDAO.Backend
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using SomeDAO.Backend.Data;
    using TonLibDotNet;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IDbProvider>();
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));
                CheckMasterAddress(db, logger, scope.ServiceProvider);
                CheckMainnet(db, logger, scope.ServiceProvider);
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(o => o.AddSystemdConsole())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void CheckMasterAddress(IDbProvider db, ILogger logger, IServiceProvider serviceProvider)
        {
            var backopt = serviceProvider.GetRequiredService<IOptions<BackendOptions>>();
            var master = backopt.Value.MasterAddress;

            if (string.IsNullOrWhiteSpace(master))
            {
                throw new InvalidOperationException("Master contract not set (in appsettings file).");
            }

            var adr = db.MainDb.Find<Settings>(Settings.MASTER_ADDRESS);
            if (adr == null)
            {
                adr = new Settings(Settings.MASTER_ADDRESS, master);
                db.MainDb.Insert(adr);
            }
            else if (!string.Equals(adr.StringValue, master, StringComparison.Ordinal))
            {
                logger.LogCritical("Master contract mismatch: saved {Address}, configured {Address}. Erase db to start with new master address!", adr.StringValue, master);
                throw new InvalidOperationException("Master contract changed");
            }

            logger.LogInformation("Master contract address: {Address}", master);
        }

        private static void CheckMainnet(IDbProvider db, ILogger logger, IServiceProvider serviceProvider)
        {
            var tonopt = serviceProvider.GetRequiredService<IOptions<TonOptions>>();
            var mainnet = tonopt.Value.UseMainnet;

            var mnet = db.MainDb.Find<Settings>(Settings.IN_MAINNET);
            if (mnet == null)
            {
                mnet = new Settings(Settings.IN_MAINNET, mainnet);
                db.MainDb.Insert(mnet);
            }
            else if (mnet.BoolValue != mainnet)
            {
                logger.LogError("Net type mismatch: saved {Address}, configured {Address}. Erase db to start with new net type!", mnet.BoolValue!.Value ? "MAINnet" : "TESTnet", mainnet ? "MAINnet" : "TESTnet");
                throw new InvalidOperationException("Net type changed");
            }

            logger.LogInformation("Net type: {Value}", mainnet ? "MAINnet" : "TESTnet");
        }
    }
}
