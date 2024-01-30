namespace SomeDAO.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OpenApi.Models;
    using RecurrentTasks;
    using SomeDAO.Backend.Data;
    using SomeDAO.Backend.Services;
    using TonLibDotNet;
    using TonLibDotNet.Types;

    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public static IReadOnlyList<Type> RegisteredTasks { get; private set; } = new List<Type>();

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);
            services.AddControllers();
            services.AddHttpClient();

            services.Configure<BackendOptions>(configuration.GetSection("BackendOptions"));

            var bo = new BackendOptions();
            configuration.GetSection("BackendOptions").Bind(bo);

            services.Configure<TonOptions>(configuration.GetSection("TonOptions"));
            services.Configure<TonOptions>(o => o.Options.KeystoreType = new KeyStoreTypeDirectory(bo.CacheDirectory));
            services.AddSingleton<ITonClient, TonClient>();

            services.AddSingleton<IDbProvider, DbProvider>();
            services.AddSingleton<DataParser>();
            services.AddSingleton<CachedData>();

            services.AddTask<DevInitService>(o => o.AutoStart(DevInitService.Interval, TimeSpan.FromSeconds(4)));
            //services.AddTask<NewOrdersDetector>(o => o.AutoStart(bo.NewOrdersDetectorInterval));
            //services.AddTask<CollectionTxTrackerService>(o => o.AutoStart(bo.CollectionTxTrackingInterval));
            //services.AddTask<MasterTxTrackerService>(o => o.AutoStart(bo.MasterTxTrackingInterval));
            //services.AddTask<OrderUpdateChecker>(o => o.AutoStart(bo.OrderUpdateCheckerInterval));
            services.AddTask<CachedData>(o => o.AutoStart(bo.SearchCacheForceReloadInterval, TimeSpan.FromSeconds(3)));
            services.AddTask<SyncTask>(o => o.AutoStart(SyncTask.Interval, TimeSpan.FromSeconds(5)));
            services.AddTask<ForceResyncTask>(o => o.AutoStart(ForceResyncTask.Interval));

            services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(o =>
            {
                o.SupportNonNullableReferenceTypes();
                o.SwaggerDoc("backend", new OpenApiInfo()
                {
                    Title = "Backend API",
                    Description = "Backend API for SomeDAO frontend.",
                    Version = "backend",
                });
                o.EnableAnnotations();

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            services.AddCors(o =>
            {
                o.AddDefaultPolicy(
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            RegisteredTasks = new List<Type>
                {
                    //typeof(ITask<NewOrdersDetector>),
                    //typeof(ITask<CollectionTxTrackerService>),
                    //typeof(ITask<MasterTxTrackerService>),
                    //typeof(ITask<OrderUpdateChecker>),
                    typeof(ITask<CachedData>),
                    typeof(ITask<SyncTask>),
                    typeof(ITask<ForceResyncTask>),
                }
                .AsReadOnly();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders();
            app.UseStatusCodePages();

            app.UseExceptionHandler(ab => ab.Run(ctx =>
            {
                ctx.Response.ContentType = "text/plain";
                return ctx.Response.WriteAsync($"Nothing here. Please enjoy StatusCode {ctx.Response.StatusCode}.");
            }));

            app.UseMiddleware<RobotsTxtMiddleware>();
            app.UseMiddleware<HealthMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/backend/swagger.json", "Backend API"));

            app.UseRouting();
            app.UseCors();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}