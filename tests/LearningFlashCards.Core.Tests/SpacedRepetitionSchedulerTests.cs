using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Study;

namespace LearningFlashCards.Core.Tests;

public class SpacedRepetitionSchedulerTests
{
    [Fact]
    public void ApplyRating_Again_SetsDueNowAndResetsState()
    {
        var state = new CardState
        {
            DueAt = DateTimeOffset.UtcNow.AddDays(3),
            IntervalDays = 5,
            EaseFactor = 2.5,
            Streak = 2,
            Lapses = 0
        };

        var now = DateTimeOffset.UtcNow;

        SpacedRepetitionScheduler.ApplyRating(state, CardReviewRating.Again, now);

        Assert.Equal(0, state.IntervalDays);
        Assert.Equal(0, state.Streak);
        Assert.Equal(1, state.Lapses);
        Assert.Equal(now, state.DueAt);
        Assert.True(state.EaseFactor <= 2.5);
    }

    [Fact]
    public void ApplyRating_Hard_IncrementsStreakAndUpdatesDueDate()
    {
        var state = new CardState
        {
            DueAt = DateTimeOffset.UtcNow,
            IntervalDays = 1,
            EaseFactor = 2.5,
            Streak = 1,
            Lapses = 0
        };

        var now = DateTimeOffset.UtcNow;

        SpacedRepetitionScheduler.ApplyRating(state, CardReviewRating.Hard, now);

        Assert.Equal(2, state.Streak);
        Assert.Equal(3, state.IntervalDays);
        Assert.Equal(now.AddDays(state.IntervalDays), state.DueAt);
    }

    [Fact]
    public void ApplyRating_Easy_RespectsEasyMinInterval()
    {
        var state = new CardState
        {
            DueAt = DateTimeOffset.UtcNow,
            IntervalDays = 1,
            EaseFactor = 2.5,
            Streak = 2,
            Lapses = 0
        };

        var settings = new DeckStudySettings
        {
            EasyMinIntervalDays = 6,
            MaxIntervalDays = 180
        };

        var now = DateTimeOffset.UtcNow;

        SpacedRepetitionScheduler.ApplyRating(state, CardReviewRating.Easy, now, settings);

        Assert.True(state.IntervalDays >= settings.EasyMinIntervalDays);
        Assert.Equal(now.AddDays(state.IntervalDays), state.DueAt);
    }

    [Fact]
    public void ApplyRating_EnforcesMaxInterval()
    {
        var state = new CardState
        {
            DueAt = DateTimeOffset.UtcNow,
            IntervalDays = 120,
            EaseFactor = 2.5,
            Streak = 5,
            Lapses = 0
        };

        var settings = new DeckStudySettings
        {
            EasyMinIntervalDays = 3,
            MaxIntervalDays = 60
        };

        var now = DateTimeOffset.UtcNow;

        SpacedRepetitionScheduler.ApplyRating(state, CardReviewRating.Medium, now, settings);

        Assert.Equal(settings.MaxIntervalDays, state.IntervalDays);
        Assert.Equal(now.AddDays(state.IntervalDays), state.DueAt);
    }

    [Fact]
    public void ApplyRating_UsesEasyMinIntervalWhenMaxIsLower()
    {
        var state = new CardState
        {
            DueAt = DateTimeOffset.UtcNow,
            IntervalDays = 2,
            EaseFactor = 2.5,
            Streak = 3,
            Lapses = 0
        };

        var settings = new DeckStudySettings
        {
            EasyMinIntervalDays = 10,
            MaxIntervalDays = 5
        };

        var now = DateTimeOffset.UtcNow;

        SpacedRepetitionScheduler.ApplyRating(state, CardReviewRating.Easy, now, settings);

        Assert.Equal(settings.EasyMinIntervalDays, state.IntervalDays);
        Assert.Equal(now.AddDays(state.IntervalDays), state.DueAt);
    }
}
