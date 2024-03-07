using SomeDAO.Backend.Data;
using SomeDAO.Backend.Services;
using TonLibDotNet;
using TonLibDotNet.Types;

namespace SomeDAO.Backend
{
    public static class StartupIndexer
    {
        public static void Configure(HostBuilderContext context, IServiceCollection services)
        {
            services.AddHttpClient();

            services.Configure<BackendOptions>(context.Configuration.GetSection("BackendOptions"));

            var bo = new BackendOptions();
            context.Configuration.GetSection("BackendOptions").Bind(bo);

            services.Configure<TonOptions>(context.Configuration.GetSection("TonOptions"));
            services.Configure<TonOptions>(o => o.Options.KeystoreType = new KeyStoreTypeDirectory(bo.CacheDirectory));
            services.AddSingleton<ITonClient, TonClient>();

            services.AddScoped<IDbProvider, DbProvider>();
            services.AddScoped<DataParser>();

            services.AddScoped<SyncSchedulerService>();
            services.AddScoped<ISearchCacheUpdater, RemoteSearchCacheUpdater>();

            services.AddTask<SyncTask>(o => o.AutoStart(SyncTask.Interval));
            services.AddTask<ForceResyncTask>(o => o.AutoStart(ForceResyncTask.Interval));
            services.AddTask<MasterTrackerTask>(o => o.AutoStart(bo.MasterSyncInterval));
            services.AddTask<TranslateTask>(o => o.AutoStart(TranslateTask.Interval));
            services.AddTask<NotificationTask>(o => o.AutoStart(NotificationTask.DefaultInterval));
        }
    }
}
