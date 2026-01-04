using System.ComponentModel.DataAnnotations;

namespace LearningFlashCards.Api.Controllers;

public record CreateUserRequest
{
    [Required]
    [StringLength(256)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [StringLength(512)]
    [Url]
    public string? AvatarUrl { get; init; }
}
