using System.ComponentModel.DataAnnotations;

namespace LearningFlashCards.Api.Controllers.Requests;

public record UpsertCardRequest
{
    public Guid? Id { get; init; }

    [Required(ErrorMessage = "Front side content is required.")]
    [StringLength(2048, MinimumLength = 1, ErrorMessage = "Front must be between 1 and 2048 characters.")]
    public string Front { get; init; } = string.Empty;

    [Required(ErrorMessage = "Back side content is required.")]
    [StringLength(2048, MinimumLength = 1, ErrorMessage = "Back must be between 1 and 2048 characters.")]
    public string Back { get; init; } = string.Empty;

    [StringLength(4096, ErrorMessage = "Notes cannot exceed 4096 characters.")]
    public string? Notes { get; init; }

    public CardStateRequest? State { get; init; }

    public IList<Guid>? TagIds { get; init; }
}

public record CardStateRequest
{
    public DateTimeOffset? DueAt { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Interval days must be non-negative.")]
    public int? IntervalDays { get; init; }

    [Range(1.3, 5.0, ErrorMessage = "Ease factor must be between 1.3 and 5.0.")]
    public double? EaseFactor { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Streak must be non-negative.")]
    public int? Streak { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Lapses must be non-negative.")]
    public int? Lapses { get; init; }
}
