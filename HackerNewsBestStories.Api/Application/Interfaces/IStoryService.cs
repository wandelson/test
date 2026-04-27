using HackerNewsBestStories.Api.Domain;

namespace HackerNewsBestStories.Api.Application.Interfaces;

public interface IStoryService
{
    Task<IReadOnlyList<StoryResponse>> GetTopStoriesAsync(int count, CancellationToken cancellationToken);
}
