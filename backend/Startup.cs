namespace SomeDAO.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using RecurrentTasks;
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
            services.AddSingleton<IDataParser, DataParser>();
            services.AddSingleton<SearchService>();
            services.AddSingleton<ISearchService>(sp => sp.GetRequiredService<SearchService>());

            services.AddTask<NewItemDetectorService>(o => o.AutoStart(bo.NewItemDetectorInterval));
            services.AddTask<CollectionTxTrackerService>(o => o.AutoStart(bo.CollectionTxTrackingInterval));
            services.AddTask<ItemUpdateChecker>(o => o.AutoStart(bo.ItemUpdateCheckerInterval));
            services.AddTask<SearchService>(o => o.AutoStart(bo.SearchCacheForceReloadInterval, TimeSpan.FromSeconds(3)));

            services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("backoffice", new OpenApiInfo()
                {
                    Title = "Backoffice API",
                    Description = "Backoffice API for SomeDAO frontend.",
                    Version = "backoffice",
                });
                o.EnableAnnotations();

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            RegisteredTasks = new List<Type>
                {
                    typeof(ITask<NewItemDetectorService>),
                }
                .AsReadOnly();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseForwardedHeaders();
            app.UseStatusCodePages();

            app.UseExceptionHandler(ab => ab.Run(ctx =>
            {
                ctx.Response.ContentType = "text/plain";
                return ctx.Response.WriteAsync($"Status Code {ctx.Response.StatusCode}");
            }));

            app.UseMiddleware<RobotsTxtMiddleware>();
            app.UseMiddleware<HealthMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/backoffice/swagger.json", "Backoffice API"));

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}