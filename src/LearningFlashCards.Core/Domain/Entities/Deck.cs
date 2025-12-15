namespace LearningFlashCards.Core.Domain.Entities;

public class Deck : BaseEntity
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IList<Card> Cards { get; set; } = new List<Card>();
    public IList<Tag> Tags { get; set; } = new List<Tag>();
}
