using RecurrentTasks;

namespace SomeDAO.Backend.Services
{
    public class LocalSeachCacheUpdater : ISearchCacheUpdater
    {
        private readonly ITask task;

        public LocalSeachCacheUpdater(ITask<CachedData> task)
        {
            this.task = task;
        }

        public Task UpdateSearchCache()
        {
            task.TryRunImmediately();

            return Task.CompletedTask;
        }
    }
}
