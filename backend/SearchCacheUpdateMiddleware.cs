using Microsoft.Extensions.Options;
using SomeDAO.Backend.Services;

namespace SomeDAO.Backend
{
    public class SearchCacheUpdateMiddleware : IMiddleware
    {
        private readonly ISearchCacheUpdater cacheUpdater;
        private readonly PathString path;

        public SearchCacheUpdateMiddleware(ISearchCacheUpdater cacheUpdater, IOptions<BackendOptions> options)
        {
            this.cacheUpdater = cacheUpdater ?? throw new ArgumentNullException(nameof(cacheUpdater));
            this.path = options?.Value.SearchCacheUpdatePath ?? default;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            return path.HasValue && path.Equals(context.Request.Path)
                ? cacheUpdater.UpdateSearchCache()
                : next(context);
        }
    }
}
