namespace LearningFlashCards.Core.Domain.Entities;

public class Card : BaseEntity
{
    public Guid DeckId { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public IList<Tag> Tags { get; set; } = new List<Tag>();
    public CardState State { get; set; } = new();
}
