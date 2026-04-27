 Hacker News Best Stories API - Architecture and Engineering Plan

## 🎯 Goal
Implement a RESTful API in ASP.NET Core that returns the **top n stories** from Hacker News, ordered by score, in an **efficient, scalable, and resilient** way without overloading the external service.

---

## 🏗️ Architecture

### Hexagonal Architecture
- **Primary Adapter (API)**: REST controller exposes `GET /api/stories/{n}` and translates HTTP requests into application commands.
- **Application Core**: contains the use case for retrieving top stories, business rules, concurrency control, heap logic, and domain models.
- **Ports**: interfaces for external dependencies, including Hacker News data retrieval, cache, and logging.
- **Secondary Adapters (Infrastructure)**: implementations of ports for HTTP client access to Hacker News, memory/Redis cache, and telemetry/logging.

### Flow
1. Client calls `GET /api/stories/{n}`.
2. API adapter invokes the application core through an input port (use case interface).
3. The use case requests best story IDs through an outbound port.
4. For each ID:
   - The core checks the cache port.
   - On a cache miss, the core calls the Hacker News story port.
   - Concurrency control is applied within the core.
5. Stories are inserted into a **min-heap** to keep only the top `n`.
6. The use case returns the ordered story list to the API adapter.
7. The API adapter serializes the response as JSON.

### Telemetry and Logging
- Use OpenTelemetry for traces, metrics, and structured logs.
- Export application logs through GELF for Graylog or compatible log collectors.
- Instrument the HTTP client, cache access, and retry/circuit-breaker paths.

### Hacker News Contract
- Best story IDs endpoint: `https://hacker-news.firebaseio.com/v0/beststories.json`
- Story details endpoint: `https://hacker-news.firebaseio.com/v0/item/{id}.json`

### API Response Model
The API returns the top `n` stories ordered by descending score using the following JSON shape:

```json
[
  {
    "title": "...",
    "uri": "...",
    "postedBy": "...",
    "time": "...",
    "score": 0,
    "commentCount": 0
  }
]
```

- `title`: story title
- `uri`: story URL
- `postedBy`: Hacker News author
- `time`: ISO 8601 publication timestamp
- `score`: story points
- `commentCount`: number of comments

### README Requirements
- Explain how to run the application.
- List any assumptions made.
- Describe enhancements or changes to implement given more time.

---

## 🔧 Algorithms and Data Structures

### Min-Heap (Priority Queue)
- Keeps only the top `n` stories.
- Complexity: `O(m log n)` for `m` stories.
- Avoids sorting the entire list.

### Cache (Memory/Redis)
- Best stories IDs: short expiration (1–2 min).
- Story details: medium expiration (5–10 min).
- Reduces external calls and latency.

### Controlled Concurrency
- `SemaphoreSlim` to limit simultaneous requests (e.g. 10).
- Prevents overloading the Hacker News API.

### Resilience
- **Polly** for retries with exponential backoff.
- Circuit breaker for repeated failures.
- Timeout configured on HttpClient.

---

## 🚀 Advanced Options

### Background Job + Snapshot
- Worker periodically fetches and updates a snapshot of the best stories.
- API only reads the snapshot → minimal latency.
- Trade-off: data may be a few seconds/minutes stale.

### Probabilistic Structures
- **Bloom Filter**: avoid redundant calls (trade-off: false positives).
- **Count-Min Sketch**: prioritize cache for more accessed stories.

---

## ⚖️ Architectural Decision

### Chosen Option
**Cache + Heap + Controlled Concurrency (+ optional Background Job)**

### Justification
- **Fewer trade-offs**: deterministic, simple to maintain.
- **Performance**: heap is efficient for top-K.
- **Scalability**: cache protects the external API.
- **Resilience**: concurrency control and retries.

### Alternatives
- Bloom Filter: interesting at massive scale, but adds complexity and false positive risk.
- Count-Min Sketch: useful for popularity, but outside the immediate scope.

---

## 🧪 Testing Strategy

### Unit Tests
- Test the application core / use case in isolation using mocked ports.
- Cover scenarios: valid `n`, `n = 0`, missing story fields, cache hit, cache miss, and error retry behavior.
- Verify min-heap logic keeps only the top `n` stories and returns them ordered by score.

### Integration Tests
- Test the API controller against a real or in-memory web host.
- Use a fake Hacker News adapter and cache adapter to simulate external responses.
- Validate the HTTP response format, status codes, and JSON schema.

### Load and Resilience Tests
- Simulate concurrent requests to verify controlled concurrency and rate limiting.
- Confirm the service does not overwhelm the Hacker News outbound port under burst traffic.
- Check retry and timeout behavior under slow or failing external calls.

### Tools
- xUnit or NUnit for unit and integration tests.
- `Microsoft.AspNetCore.Mvc.Testing` for end-to-end API host tests.
- `Moq` or `NSubstitute` for port mocks.
- `BenchmarkDotNet` / load tools for performance profiling if needed.
- OpenTelemetry SDK for tracing, metrics, and structured log collection.
- GELF exporter for log shipping to Graylog or compatible collectors.

---

## 📖 README.md (Summary)

### How to run
```bash
git clone https://github.com/yourusername/HackerNewsBestStoriesAPI.git
cd HackerNewsBestStoriesAPI
dotnet run