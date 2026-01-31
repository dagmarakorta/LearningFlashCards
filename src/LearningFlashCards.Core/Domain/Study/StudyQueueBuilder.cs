using LearningFlashCards.Core.Domain.Entities;

namespace LearningFlashCards.Core.Domain.Study;

public static class StudyQueueBuilder
{
    public static IReadOnlyList<Card> SelectDue(IEnumerable<Card> cards, DateTimeOffset now, int dailyLimit)
    {
        if (cards is null)
        {
            throw new ArgumentNullException(nameof(cards));
        }

        var dueCards = cards
            .Where(card => card.State.DueAt <= now)
            .OrderBy(card => card.State.DueAt)
            .ToList();

        if (dailyLimit > 0 && dueCards.Count > dailyLimit)
        {
            dueCards = dueCards.Take(dailyLimit).ToList();
        }

        return dueCards;
    }
}
