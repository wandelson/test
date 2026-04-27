# Hacker News Best Stories API

This workspace contains a minimal ASP.NET Core implementation of a Hacker News Best Stories API, plus design and planning documentation.

## Requirements

Using ASP.NET Core, implement a RESTful API to retrieve the details of the best `n` stories from the Hacker News API, as determined by their score, where `n` is specified by the caller to the API.

The Hacker News API is documented here: https://github.com/HackerNews/API.

- Story IDs can be retrieved from: https://hacker-news.firebaseio.com/v0/beststories.json
- Story details can be retrieved from: https://hacker-news.firebaseio.com/v0/item/21233041.json (example for story ID `21233041`)

The API should return an array of the best `n` stories as returned by the Hacker News API, ordered by descending score, in the following form:

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  },
  { ... },
  { ... },
  { ... }
]
```

## Architecture

The implementation follows a hexagonal-style architecture with three main layers:

1. **API Layer**: `StoriesController` is the entry point for HTTP requests. It maps `GET /api/stories/{count}` to the story retrieval use case and returns JSON.

2. **Application Layer**: `IStoryService` defines the contract for retrieving stories. `StoryService` implements the core use case: fetch top best stories, filter, and return the highest scored items. The service contains the logic for caching, concurrency control, and selecting the top stories.

3. **Infrastructure Layer**: `IHackerNewsClient` defines the contract for external Hacker News data retrieval. `HackerNewsClient` implements the HTTP calls to Hacker News endpoints. Configuration and models live in `Infrastructure/Options` and `Infrastructure/Models`.

### Data Flow
1. Client requests `GET /api/stories/{count}`.
2. `StoriesController` validates the request and calls `IStoryService.GetBestStoriesAsync(count)`.
3. `StoryService` requests the list of best story IDs from `IHackerNewsClient`.
4. For each ID, the service either reads cached story details or fetches from Hacker News and caches the result.
5. The service uses an internal selection strategy to keep only the top `count` stories by score.
6. The result is returned to the controller and serialized as JSON.

### Key Features
- **Caching**: Best story IDs and individual story details are cached to reduce repeated calls.
- **Concurrency Control**: `SemaphoreSlim` limits concurrent HTTP calls to protect the external service.
- **Resilience**: Retry and circuit breaker patterns with Polly, timeouts, and error handling.
- **Observability**: OpenTelemetry for tracing, metrics, and structured logs with GELF for Graylog.

## How to run
```bash
dotnet run --project HackerNewsBestStories.Api/HackerNewsBestStories.Api.csproj
```

The API endpoint is:
- `GET /api/stories/{count}`

Example:
- `GET /api/stories/10`

## Implementation details
- Hexagonal-style separation: controller, service, and infrastructure.
- Caches top story IDs for 1 minute and story details for 10 minutes.
- Uses `SemaphoreSlim` to limit concurrent Hacker News item fetches.
- Uses Polly retry and circuit breaker policies for HTTP resilience.
- Uses OpenTelemetry for tracing and logs with Serilog + GELF sink.

## Assumptions
- The Hacker News best stories endpoint is the primary source of story IDs.
- Story details may be stale for a short period due to caching.
- The API is intentionally small and focused on the problem statement rather than a full production-grade platform.
- Only items with `type: "story"` are considered.
- If a story has no `url`, `uri` is set to `null`.
- `time` is converted from Unix timestamp to ISO 8601 in UTC.
- For invalid `n` (zero or negative), an empty array is returned.
- On fetch failures, stories are skipped to return partial results.
- Memory cache is used; Redis support is not implemented.
- Concurrency limit is set to 10 simultaneous requests.
- Endpoint is fixed as `GET /api/stories/{n}`.

## Future Enhancements
- Add a dedicated cache adapter for Redis or distributed cache.
- Implement background snapshot refresh to pre-warm top story data.
- Add more comprehensive integration tests for external dependency behavior.
- Extend logging and metrics collection for production observability.
- Support query parameters like `?count=n` for the endpoint.
- Use probabilistic structures like Bloom Filters for optimization.
