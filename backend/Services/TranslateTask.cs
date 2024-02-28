using DeepL;
using Microsoft.Extensions.Options;
using RecurrentTasks;
using SomeDAO.Backend.Data;

namespace SomeDAO.Backend.Services
{
    public class TranslateTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan HaveMoreDataInterval = TimeSpan.FromSeconds(5);

        private const int MaxBatch = 50;

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly BackendOptions options;

        private readonly TextTranslateOptions translateOptions = new() { PreserveFormatting = true };

        private ITranslator? translator;
        private List<Language> languages = new();

        public TranslateTask(ILogger<TranslateTask> logger, IDbProvider dbProvider, IOptions<BackendOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(options.DeeplToken))
            {
                logger.LogWarning("DeepL token not set. Translation disabled.");
                return;
            }

            currentTask.Options.Interval = Interval;

            this.languages = dbProvider.MainDb.Table<Language>().ToList();

            var (count1, haveMore1) = await TranslateOrders().ConfigureAwait(false);
            var (count2, haveMore2) = await TranslateUsers().ConfigureAwait(false);

            logger.LogDebug("Processed {Count} orders and {Count} users", count1, count2);

            if (haveMore1 || haveMore2)
            {
                currentTask.Options.Interval = HaveMoreDataInterval;
            }
        }

        protected ITranslator GetTranslator()
        {
            translator ??= new DeepL.Translator(options.DeeplToken);
            return translator;
        }

        protected async Task<(int translated, bool haveMore)> TranslateOrders()
        {
            var count = 0;

            do
            {
                var item = dbProvider.MainDb.Table<Order>().FirstOrDefault(x => x.NeedTranslation);
                if (item == null)
                {
                    return (count, false);
                }

                await EnsureTranslated(item.Name, item.NameHash, item.Language).ConfigureAwait(false);
                await EnsureTranslated(item.Description, item.DescriptionHash, item.Language).ConfigureAwait(false);
                await EnsureTranslated(item.TechnicalTask, item.TechnicalTaskHash, item.Language).ConfigureAwait(false);

                item.NeedTranslation = false;
                dbProvider.MainDb.Update(item);
                count++;
            }
            while (count < MaxBatch);

            return (count, true);
        }

        protected async Task<(int translated, bool haveMore)> TranslateUsers()
        {
            var count = 0;

            do
            {
                var item = dbProvider.MainDb.Table<User>().FirstOrDefault(x => x.NeedTranslation);
                if (item == null)
                {
                    return (count, false);
                }

                await EnsureTranslated(item.About, item.AboutHash, item.Language).ConfigureAwait(false);

                item.NeedTranslation = false;
                dbProvider.MainDb.Update(item);
                count++;
            }
            while (count < MaxBatch);

            return (count, true);
        }

        protected async Task EnsureTranslated(string? value, byte[]? hash, string? originalLanguageHash)
        {
            if (string.IsNullOrWhiteSpace(value) || hash == null || hash.Length == 0)
            {
                return;
            }

            var existing = dbProvider.MainDb.Table<Translation>().Where(x => x.Hash == hash).ToList();
            foreach (var language in languages.Where(x => x.Hash != originalLanguageHash).Select(x => x.Name))
            {
                if (!existing.Exists(x => x.Language == language))
                {
                    // fix for: targetLanguageCode="en" is deprecated, please use "en-GB" or "en-US" instead
                    var toLang = language == "en" ? "en-GB" : language;

                    var text = await GetTranslator().TranslateTextAsync(value, null, toLang, translateOptions).ConfigureAwait(false);
                    var t = new Translation()
                    {
                        Hash = hash,
                        Language = language,
                        TranslatedText = text.Text,
                        Timestamp = DateTimeOffset.UtcNow,
                    };
                    dbProvider.MainDb.Insert(t);

                    var td = t.TranslatedText.Length > 50 ? t.TranslatedText[..50] + "…" : t.TranslatedText;
                    logger.LogDebug("Translated {Lang}/{Hash}: {Value}", language, Convert.ToBase64String(hash), td);
                }
            }
        }
    }
}
