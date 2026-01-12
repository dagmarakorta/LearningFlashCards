using System.ComponentModel.DataAnnotations;

namespace LearningFlashCards.Api.Controllers.Requests;

public record UpsertTagRequest
{
    public Guid? Id { get; init; }

    [Required(ErrorMessage = "Tag name is required.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 128 characters.")]
    public string Name { get; init; } = string.Empty;
}
