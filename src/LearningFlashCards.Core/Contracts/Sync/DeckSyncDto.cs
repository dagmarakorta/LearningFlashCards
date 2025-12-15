using LearningFlashCards.Core.Domain.Entities;

namespace LearningFlashCards.Core.Contracts.Sync;

public class DeckSyncDto
{
    public Deck Deck { get; set; } = new();
    public IList<Card> Cards { get; set; } = new List<Card>();
}
