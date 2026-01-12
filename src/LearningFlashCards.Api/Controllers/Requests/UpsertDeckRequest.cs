using System.ComponentModel.DataAnnotations;

namespace LearningFlashCards.Api.Controllers.Requests;

public record UpsertDeckRequest
{
    public Guid? Id { get; init; }

    [Required(ErrorMessage = "Deck name is required.")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 256 characters.")]
    public string Name { get; init; } = string.Empty;

    [StringLength(1024, ErrorMessage = "Description cannot exceed 1024 characters.")]
    public string? Description { get; init; }
}
