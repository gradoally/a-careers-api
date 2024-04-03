namespace SomeDAO.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.Json.Serialization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OpenApi.Models;
    using RecurrentTasks;
    using SomeDAO.Backend.Data;
    using SomeDAO.Backend.Services;

    public class StartupApi
    {
        private readonly IConfiguration configuration;

        public StartupApi(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public static IReadOnlyList<Type> RegisteredTasks { get; private set; } = [];

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);
            services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
            services
                .AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase, allowIntegerValues: false)));

            var optionsSection = configuration.GetSection("BackendOptions");
            services.Configure<BackendOptions>(optionsSection);

            var bo = new BackendOptions();
            optionsSection.Bind(bo);

            services.AddScoped<IDbProvider, DbProvider>();
            services.AddScoped(sp => new Lazy<IDbProvider>(() => sp.GetRequiredService<IDbProvider>()));
            services.AddSingleton<CachedData>();
            services.AddScoped<ISearchCacheUpdater, LocalSeachCacheUpdater>();
            services.AddSingleton<SearchCacheUpdateMiddleware>();
            services.AddSingleton<IndexerControlTask>();
            services.AddSingleton<IndexerHealthUpdateMiddleware>();

            services.AddTask<CachedData>(o => o.AutoStart(bo.SearchCacheForceReloadInterval, TimeSpan.FromSeconds(1)));
            services.AddTask<IndexerControlTask>(o => o.AutoStart(IndexerControlTask.Interval, TimeSpan.FromSeconds(5)));

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
                    typeof(ITask<CachedData>),
                    typeof(ITask<IndexerControlTask>),
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
            app.UseMiddleware<SearchCacheUpdateMiddleware>();
            app.UseMiddleware<IndexerHealthUpdateMiddleware>();

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