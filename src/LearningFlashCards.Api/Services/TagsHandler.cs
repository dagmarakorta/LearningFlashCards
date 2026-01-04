using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Services;

public class TagsHandler
{
    private readonly ITagRepository _tagRepository;

    public TagsHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<HandlerResult<IReadOnlyList<Tag>>> GetTagsAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var tags = await _tagRepository.GetByOwnerAsync(ownerId, cancellationToken);
        return HandlerResult<IReadOnlyList<Tag>>.Success(tags);
    }

    public async Task<HandlerResult<Tag>> UpsertTagAsync(Tag tag, Guid ownerId, CancellationToken cancellationToken)
    {
        if (tag.OwnerId == Guid.Empty)
        {
            tag.OwnerId = ownerId;
        }
        else if (tag.OwnerId != ownerId)
        {
            return HandlerResult<Tag>.Forbidden();
        }

        tag.ModifiedAt = DateTimeOffset.UtcNow;
        await _tagRepository.UpsertAsync(tag, cancellationToken);
        return HandlerResult<Tag>.Success(tag);
    }

    public async Task<HandlerResult<string?>> SoftDeleteTagAsync(Guid tagId, Guid ownerId, CancellationToken cancellationToken)
    {
        var existing = await _tagRepository.GetAsync(tagId, cancellationToken);
        if (existing is null || existing.DeletedAt != null || existing.OwnerId != ownerId)
        {
            return HandlerResult<string?>.NotFound();
        }

        var deletedAt = DateTimeOffset.UtcNow;
        await _tagRepository.SoftDeleteAsync(tagId, deletedAt, cancellationToken);
        return HandlerResult<string?>.Success(null, StatusCodes.Status204NoContent);
    }
}
