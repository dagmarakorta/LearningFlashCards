using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Study;

namespace LearningFlashCards.Core.Tests;

public class StudyQueueBuilderTests
{
    [Fact]
    public void SelectDue_FiltersByDueDateAndOrders()
    {
        var now = DateTimeOffset.UtcNow;
        var cards = new[]
        {
            new Card { State = new CardState { DueAt = now.AddDays(2) } },
            new Card { State = new CardState { DueAt = now.AddDays(-1) } },
            new Card { State = new CardState { DueAt = now } }
        };

        var result = StudyQueueBuilder.SelectDue(cards, now, 0).ToList();

        Assert.Equal(2, result.Count);
        Assert.True(result[0].State.DueAt <= result[1].State.DueAt);
        Assert.All(result, card => Assert.True(card.State.DueAt <= now));
    }

    [Fact]
    public void SelectDue_AppliesDailyLimit()
    {
        var now = DateTimeOffset.UtcNow;
        var cards = Enumerable.Range(0, 5)
            .Select(index => new Card { State = new CardState { DueAt = now.AddMinutes(index * -1) } })
            .ToList();

        var result = StudyQueueBuilder.SelectDue(cards, now, 2);

        Assert.Equal(2, result.Count);
        Assert.Equal(cards[4].State.DueAt, result[0].State.DueAt);
    }
}
