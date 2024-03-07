using Microsoft.Extensions.Options;
using SomeDAO.Backend.Services;

namespace SomeDAO.Backend
{
    public class SearchCacheUpdateMiddleware : IMiddleware
    {
        private readonly PathString path;

        public SearchCacheUpdateMiddleware(IOptions<BackendOptions> options)
        {
            this.path = options?.Value.SearchCacheUpdatePath ?? default;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!path.HasValue || !path.Equals(context.Request.Path))
            {
                return next(context);
            }

            var cacheUpdater = context.RequestServices.GetRequiredService<ISearchCacheUpdater>();
            return cacheUpdater.UpdateSearchCache();
        }
    }
}
