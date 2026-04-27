# Santander - Developer Coding Test

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

In addition to the above, your API should efficiently service a large number of requests without risking overload of the Hacker News API.

You should share a public repository with us that includes a `README.md` file describing how to run the application, any assumptions you made, and any enhancements or changes you would make given more time.