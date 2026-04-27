using HackerNewsBestStories.Api.Infrastructure.Models;

namespace HackerNewsBestStories.Api.Infrastructure.HackerNews;

public interface IHackerNewsClient
{
    Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken);
    Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken);
}
