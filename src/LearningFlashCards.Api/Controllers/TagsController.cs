using LearningFlashCards.Api.Controllers.Requests;
using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LearningFlashCards.Api.Controllers;

[Route("api/[controller]")]
public class TagsController : ApiControllerBase
{
    private readonly TagsHandler _tagsHandler;

    public TagsController(TagsHandler tagsHandler)
    {
        _tagsHandler = tagsHandler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Tag>>> GetTags(CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _tagsHandler.GetTagsAsync(ownerId, cancellationToken);
        return StatusCode(result.StatusCode, result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<Tag>> UpsertTag([FromBody] UpsertTagRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var tag = MapToTag(request);
        var result = await _tagsHandler.UpsertTagAsync(tag, ownerId, cancellationToken);
        return ToActionResult(result);
    }

    private static Tag MapToTag(UpsertTagRequest request)
    {
        return new Tag
        {
            Id = request.Id ?? Guid.NewGuid(),
            Name = TextSanitizer.SanitizePermissive(request.Name)
        };
    }

    [HttpDelete("{tagId:guid}")]
    public async Task<IActionResult> SoftDeleteTag(Guid tagId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _tagsHandler.SoftDeleteTagAsync(tagId, ownerId, cancellationToken);
        return result.IsSuccess ? StatusCode(result.StatusCode) : StatusCode(result.StatusCode, result.Error);
    }

    private ActionResult ToActionResult<T>(HandlerResult<T> result)
    {
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        return StatusCode(result.StatusCode, result.Value);
    }
}
