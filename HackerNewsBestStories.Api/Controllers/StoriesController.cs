using HackerNewsBestStories.Api.Application.Interfaces;
using HackerNewsBestStories.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsBestStories.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StoriesController : ControllerBase
{
    private readonly IStoryService _storyService;

    public StoriesController(IStoryService storyService)
    {
        _storyService = storyService;
    }

    [HttpGet("{count:int}")]
    public async Task<IActionResult> Get(int count, CancellationToken cancellationToken)
    {
        if (count < 1 || count > 500)
        {
            return BadRequest("Count must be between 1 and 500.");
        }

        var stories = await _storyService.GetTopStoriesAsync(count, cancellationToken);
        return Ok(stories);
    }
}
