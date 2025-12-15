namespace LearningFlashCards.Core.Domain.Entities;

public class UserProfile : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? LastSyncToken { get; set; }
}
