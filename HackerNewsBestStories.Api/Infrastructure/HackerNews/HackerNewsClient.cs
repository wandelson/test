using System.Net.Http.Json;
using HackerNewsBestStories.Api.Infrastructure.Models;

namespace HackerNewsBestStories.Api.Infrastructure.HackerNews;

public sealed class HackerNewsClient : IHackerNewsClient
{
    private readonly HttpClient _client;
    private readonly ILogger<HackerNewsClient> _logger;

    public HackerNewsClient(HttpClient client, ILogger<HackerNewsClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        var result = await _client.GetFromJsonAsync<List<int>>("beststories.json", cancellationToken);
        return result ?? new List<int>();
    }

    public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await _client.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Hacker News item {ItemId}", id);
            return null;
        }
    }
}
