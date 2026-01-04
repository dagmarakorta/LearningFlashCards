using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LearningFlashCards.Api.Controllers;

[Route("api/[controller]")]
public class TagsController : ApiControllerBase
{
    private readonly ITagRepository _tagRepository;

    public TagsController(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Tag>>> GetTags(CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var tags = await _tagRepository.GetByOwnerAsync(ownerId, cancellationToken);
        return Ok(tags);
    }

    [HttpPost]
    public async Task<ActionResult<Tag>> UpsertTag([FromBody] Tag tag, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        if (tag.OwnerId == Guid.Empty)
        {
            tag.OwnerId = ownerId;
        }
        else if (tag.OwnerId != ownerId)
        {
            return Forbid();
        }

        tag.ModifiedAt = DateTimeOffset.UtcNow;
        await _tagRepository.UpsertAsync(tag, cancellationToken);
        return Ok(tag);
    }

    [HttpDelete("{tagId:guid}")]
    public async Task<IActionResult> SoftDeleteTag(Guid tagId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var existing = await _tagRepository.GetAsync(tagId, cancellationToken);
        if (existing is null || existing.DeletedAt != null || existing.OwnerId != ownerId)
        {
            return NotFound();
        }

        var deletedAt = DateTimeOffset.UtcNow;
        await _tagRepository.SoftDeleteAsync(tagId, deletedAt, cancellationToken);
        return NoContent();
    }
}
