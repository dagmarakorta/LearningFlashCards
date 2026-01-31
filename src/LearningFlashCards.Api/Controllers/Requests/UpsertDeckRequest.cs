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

    [Range(1, int.MaxValue, ErrorMessage = "Daily review limit must be at least 1.")]
    public int? DailyReviewLimit { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Easy minimum interval must be at least 1 day.")]
    public int? EasyMinIntervalDays { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Max interval must be at least 1 day.")]
    public int? MaxIntervalDays { get; init; }

    public bool? RepeatInSession { get; init; }
}
