namespace LearningFlashCards.Core.Domain.Entities;

public class Tag : BaseEntity
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
}
