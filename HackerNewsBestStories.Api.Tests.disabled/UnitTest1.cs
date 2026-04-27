using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading;
using HackerNewsBestStories.Api.Application.Interfaces;
using HackerNewsBestStories.Api.Application.Services;
using HackerNewsBestStories.Api.Domain;
using HackerNewsBestStories.Api.Infrastructure.HackerNews;
using HackerNewsBestStories.Api.Infrastructure.Models;
using HackerNewsBestStories.Api.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace HackerNewsBestStories.Api.Tests;

public sealed class StoryServiceTests
{
    [Test]
    public async Task GetTopStoriesAsync_ReturnsEmpty_WhenCountIsZero()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var client = new FakeHackerNewsClient(new[] { 1, 2 }, new Dictionary<int, HackerNewsItem>());
        var options = Options.Create(new HackerNewsOptions
        {
            BestStoriesCacheMinutes = 5,
            StoryCacheMinutes = 5,
            MaxConcurrentRequests = 2,
            MaxStoryCount = 500
        });

        var service = new StoryService(memoryCache, client, options, NullLogger<StoryService>.Instance);
        var result = await service.GetTopStoriesAsync(0, CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetTopStoriesAsync_ReturnsStoriesSortedByScore()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var client = new FakeHackerNewsClient(
            new[] { 10, 20, 30 },
            new Dictionary<int, HackerNewsItem>
            {
                [10] = new HackerNewsItem(10, "Low Score", "https://example.com/1", "alice", 1672531200, 10, 0, "story"),
                [20] = new HackerNewsItem(20, "High Score", "https://example.com/2", "bob", 1672531200, 50, 5, "story"),
                [30] = new HackerNewsItem(30, "Medium Score", "https://example.com/3", "carol", 1672531200, 30, 2, "story")
            });
        var options = Options.Create(new HackerNewsOptions
        {
            BestStoriesCacheMinutes = 5,
            StoryCacheMinutes = 5,
            MaxConcurrentRequests = 2,
            MaxStoryCount = 500
        });

        var service = new StoryService(memoryCache, client, options, NullLogger<StoryService>.Instance);
        var result = await service.GetTopStoriesAsync(2, CancellationToken.None);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Title, Is.EqualTo("High Score"));
        Assert.That(result[1].Title, Is.EqualTo("Medium Score"));
    }
}

public sealed class StoriesControllerTests
{
    private WebApplicationFactory<Program> _factory = new();

    [Test]
    public async Task Get_ReturnsBadRequest_WhenCountIsInvalid()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/stories/0");

        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Get_ReturnsOkAndStoryList_WhenCountIsValid()
    {
        var fakeClient = new FakeHackerNewsClient(
            new[] { 100, 101 },
            new Dictionary<int, HackerNewsItem>
            {
                [100] = new HackerNewsItem(100, "Test Story 1", "https://example.com/100", "test", 1672531200, 10, 1, "story"),
                [101] = new HackerNewsItem(101, "Test Story 2", "https://example.com/101", "test", 1672531200, 20, 2, "story")
            });

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHackerNewsClient>();
                services.AddSingleton<IHackerNewsClient>(fakeClient);
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/stories/2");

        response.EnsureSuccessStatusCode();
        var stories = await response.Content.ReadFromJsonAsync<List<StoryResponse>>();

        Assert.That(stories, Is.Not.Null);
        Assert.That(stories!.Count, Is.EqualTo(2));
        Assert.That(stories[0].Title, Is.EqualTo("Test Story 2"));
        Assert.That(stories[1].Title, Is.EqualTo("Test Story 1"));
    }
}

internal sealed class FakeHackerNewsClient : IHackerNewsClient
{
    private readonly int[] _ids;
    private readonly Dictionary<int, HackerNewsItem> _items;

    public FakeHackerNewsClient(IEnumerable<int> ids, Dictionary<int, HackerNewsItem> items)
    {
        _ids = ids.ToArray();
        _items = items;
    }

    public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult((IReadOnlyList<int>)_ids);
    }

    public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        _items.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }
}
