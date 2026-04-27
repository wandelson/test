# Final Plan

## Objective
Implement a Hacker News Best Stories API in ASP.NET Core using a clean hexagonal architecture, with caching, controlled concurrency, resilience, and observability.

## Current Workspace
- `spec.md`: architecture and implementation plan
- `story.md`: assignment description and API contract
- `questions.md`: clarification questions
- `instructions.md`: translated work instructions

## Tasks
1. Finalize the design in `spec.md` by keeping the hexagonal architecture and API response contract.
2. Create the ASP.NET Core implementation using:
   - `GET /api/stories/{n}` or equivalent endpoint
   - Hacker News endpoints: `beststories.json` and `item/{id}.json`
   - caching for IDs and story details
   - controlled concurrency with `SemaphoreSlim`
   - Polly retries, timeout, and circuit breaker
   - OpenTelemetry instrumentation and GELF logging
3. Add unit and integration tests for core use cases:
   - valid `n`
   - zero and out-of-range values
   - cache hit/miss behavior
   - error resilience and fallback handling
4. Write `README.md` with:
   - how to run the app
   - assumptions made
   - enhancements for more time

## Review Section
The plan focuses on minimal, high-value changes and avoids unnecessary complexity. It aligns with the sample online solutions by using caching, throttled outbound calls, and robust error handling.

### Next step
Implement the project scaffold and the first working use case for fetching top `n` stories.
