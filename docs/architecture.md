# Solution Architecture

## Overview
This repository implements a minimal ASP.NET Core Web API that returns the top `n` best stories from Hacker News. The design focuses on clean separation of concerns, resilience, controlled concurrency, and caching to avoid overloading the external Hacker News service.

## Project Structure
- `HackerNewsBestStories.Api/`
  - ASP.NET Core Web API project
  - `Controllers/StoriesController.cs` exposes the REST endpoint
  - `Application/Services/StoryService.cs` contains the business logic
  - `Infrastructure/HackerNews/` contains the external Hacker News HTTP client
  - `Infrastructure/Models/` contains the Hacker News item data model
  - `Infrastructure/Options/` contains configuration classes
- `HackerNewsBestStories.Api.Tests/`
  - Unit tests for the application and controller behavior

## Architecture Style
The implementation follows a hexagonal-style architecture with three main layers:

1. API Layer
   - `StoriesController` is the entry point for HTTP requests.
   - It maps `GET /api/stories/{count}` to the story retrieval use case and returns JSON.

2. Application Layer
   - `IStoryService` defines the contract for retrieving stories.
   - `StoryService` implements the core use case: fetch top best stories, filter, and return the highest scored items.
   - The service contains the logic for caching, concurrency control, and selecting the top stories.

3. Infrastructure Layer
   - `IHackerNewsClient` defines the contract for external Hacker News data retrieval.
   - `HackerNewsClient` implements the HTTP calls to Hacker News endpoints.
   - Configuration and models live in `Infrastructure/Options` and `Infrastructure/Models`.

## Data Flow
1. Client requests `GET /api/stories/{count}`.
2. `StoriesController` validates the request and calls `IStoryService.GetBestStoriesAsync(count)`.
3. `StoryService` requests the list of best story IDs from `IHackerNewsClient`.
4. For each ID, the service either:
   - reads cached story details, or
   - fetches the story from Hacker News and caches the result.
5. The service uses an internal selection strategy to keep only the top `count` stories by score.
6. The result is returned to the controller and serialized as JSON.

## Caching and Performance
- Best story IDs and individual story details are cached to reduce repeated calls.
- Caching improves latency and shields the Hacker News API from repeated requests.
- The design is optimized to avoid sorting the entire dataset when only the top `n` stories are needed.
- `SemaphoreSlim` is used to limit concurrent HTTP calls and protect the external service.

## Resilience
- The implementation is designed to be fault tolerant.
- Retry and circuit breaker patterns are applied to outbound HTTP requests.
- Timeouts and error handling prevent slow or failing external calls from blocking the API.
- Logging and telemetry are integrated so failures can be diagnosed.

## Domain Model
The API returns a simplified story view containing:
- `title`
- `uri`
- `postedBy`
- `time`
- `score`
- `commentCount`

This model is derived from the Hacker News item contract and is shaped for client consumption.

## Deployment and Running
- Run the API using `dotnet run --project HackerNewsBestStories.Api/HackerNewsBestStories.Api.csproj`.
- The endpoint is `GET /api/stories/{count}`.
- Example: `GET /api/stories/10` returns the top 10 stories.

## Assumptions
- The Hacker News best stories endpoint is the primary source of story IDs.
- Story details may be stale for a short period due to caching.
- The API is intentionally small and focused on the problem statement rather than a full production-grade platform.

## Future Improvements
- Add a dedicated cache adapter for Redis or distributed cache.
- Implement background snapshot refresh to pre-warm top story data.
- Add more comprehensive integration tests for external dependency behavior.
- Extend logging and metrics collection for production observability.
