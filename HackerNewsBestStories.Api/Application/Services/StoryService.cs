using HackerNewsBestStories.Api.Application.Interfaces;
using HackerNewsBestStories.Api.Domain;
using HackerNewsBestStories.Api.Infrastructure.HackerNews;
using HackerNewsBestStories.Api.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNewsBestStories.Api.Application.Services;

public sealed class StoryService : IStoryService
{
    private readonly IMemoryCache _cache;
    private readonly IHackerNewsClient _client;
    private readonly HackerNewsOptions _options;
    private readonly ILogger<StoryService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public StoryService(
        IMemoryCache cache,
        IHackerNewsClient client,
        IOptions<HackerNewsOptions> options,
        ILogger<StoryService> logger)
    {
        _cache = cache;
        _client = client;
        _options = options.Value;
        _logger = logger;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests);
    }

    public async Task<IReadOnlyList<StoryResponse>> GetTopStoriesAsync(int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return Array.Empty<StoryResponse>();
        }

        count = Math.Min(count, _options.MaxStoryCount);

        var ids = await _cache.GetOrCreateAsync(CacheKeys.BestStoryIds, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.BestStoriesCacheMinutes);
            return await _client.GetBestStoryIdsAsync(cancellationToken);
        }) ?? Array.Empty<int>();

        // Simpler: fetch only the top count * 2 IDs to cover potential non-stories or missing data
        var topIds = ids.Take(count * 2).ToArray();

        var tasks = topIds
            .Select(id => GetStoryAsync(id, cancellationToken));

        var stories = await Task.WhenAll(tasks);

        return stories
            .Where(s => s is not null)
            .OrderByDescending(story => story.Score)
            .Take(count)
            .ToArray();
    }

    private async Task<StoryResponse?> GetStoryAsync(int id, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.StoryDetails(id);

        if (_cache.TryGetValue(cacheKey, out StoryResponse? cached) && cached is not null)
        {
            return cached;
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var item = await _client.GetItemAsync(id, cancellationToken);

            if (item is null || item.Type != "story")
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(item.Title))
            {
                return null;
            }

            var title = item.Title!;
            var story = new StoryResponse(
                title,
                item.Url,
                item.By ?? "unknown",
                DateTimeOffset.FromUnixTimeSeconds(item.Time),
                item.Score,
                item.Descendants);

            _cache.Set(cacheKey, story, TimeSpan.FromMinutes(_options.StoryCacheMinutes));
            return story;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to retrieve item {ItemId}", id);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static class CacheKeys
    {
        public const string BestStoryIds = "best-story-ids";

        public static string StoryDetails(int id) => $"story-details-{id}";
    }
}
