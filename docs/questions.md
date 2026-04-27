# Clarification Questions

1. Should the API only return items with `type: "story"`, or should it include any item returned by Hacker News?
2. If a story has no `url`, should `uri` be `null`, omitted, or should we use a fallback value?
3. Should `time` be converted from the Hacker News Unix timestamp to ISO 8601 in UTC?
4. How should the API behave when `n` is zero or larger than the number of available best stories?
5. If a Hacker News story fetch fails or returns invalid data, should the API retry, skip the story, or return an error?
6. Do you expect memory cache only, or should the design support Redis as a configurable cache backend?
7. What is the acceptable concurrency limit for outbound Hacker News requests? Should this be configurable?
8. Should the API return partial results when some external calls fail, or should it fail the entire request?
9. Should the service use a background snapshot approach in production, or is on-demand retrieval preferred for this implementation?
10. Do you want the endpoint path fixed as `GET /api/stories/{n}` or should query parameters like `?count=n` also be supported?