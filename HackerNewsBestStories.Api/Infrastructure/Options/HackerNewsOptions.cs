namespace HackerNewsBestStories.Api.Infrastructure.Options;

public sealed class HackerNewsOptions
{
    public string BaseUrl { get; init; } = "https://hacker-news.firebaseio.com/v0/";
    public int BestStoriesCacheMinutes { get; init; } = 1;
    public int StoryCacheMinutes { get; init; } = 10;
    public int MaxConcurrentRequests { get; init; } = 10;
    public int MaxStoryCount { get; init; } = 500;
}
