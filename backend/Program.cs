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
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));

                var db = scope.ServiceProvider.GetRequiredService<IDbProvider>();

                // check master address
                var backopt = scope.ServiceProvider.GetRequiredService<IOptions<BackendOptions>>();
                var master = backopt.Value.MasterAddress;
                var adr = await db.MainDb.FindAsync<Settings>(Settings.MASTER_ADDRESS).ConfigureAwait(false);
                if (adr == null)
                {
                    adr = new Settings(Settings.MASTER_ADDRESS, master);
                    await db.MainDb.InsertAsync(adr).ConfigureAwait(false);
                    logger.LogInformation("Master contract enabled: {Address}", master);
                }
                else if (adr.StringValue != master)
                {
                    logger.LogError("Master contract mismatch: saved {Address}, configured {Address}. Erase db to start with new master address!", adr.StringValue, master);
                    throw new InvalidOperationException("Master contract changed");
                }
                else
                {
                    logger.LogInformation("Master contract address: {Address}", master);
                }

                // check mainnet/testnet
                var tonopt = scope.ServiceProvider.GetRequiredService<IOptions<TonOptions>>();
                var mainnet = tonopt.Value.UseMainnet;
                var mnet = await db.MainDb.FindAsync<Settings>(Settings.IN_MAINNET).ConfigureAwait(false);
                if (mnet == null)
                {
                    mnet = new Settings(Settings.IN_MAINNET, mainnet);
                    await db.MainDb.InsertAsync(mnet).ConfigureAwait(false);
                    logger.LogInformation("Net type enabled: {Value}", mainnet ? "MAINnet" : "TESTnet");
                }
                else if (mnet.BoolValue != mainnet)
                {
                    logger.LogError("Net type mismatch: saved {Address}, configured {Address}. Erase db to start with new net type!", mnet.BoolValue!.Value ? "MAINnet" : "TESTnet", mainnet ? "MAINnet" : "TESTnet");
                    throw new InvalidOperationException("Net type changed");
                }
                else
                {
                    logger.LogInformation("Net type: {Value}", mainnet ? "MAINnet" : "TESTnet");
                }

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
    }
}
