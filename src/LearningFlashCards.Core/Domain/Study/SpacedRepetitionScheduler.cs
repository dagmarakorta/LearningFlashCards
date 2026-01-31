using LearningFlashCards.Core.Domain.Entities;

namespace LearningFlashCards.Core.Domain.Study;

public static class SpacedRepetitionScheduler
{
    public static void ApplyRating(
        CardState state,
        CardReviewRating rating,
        DateTimeOffset now,
        DeckStudySettings? settings = null)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var effectiveSettings = settings ?? new DeckStudySettings();
        var easyMinInterval = Math.Max(1, effectiveSettings.EasyMinIntervalDays);
        var maxInterval = Math.Max(easyMinInterval, effectiveSettings.MaxIntervalDays);

        var quality = rating switch
        {
            CardReviewRating.Again => 1,
            CardReviewRating.Hard => 3,
            CardReviewRating.Medium => 4,
            CardReviewRating.Easy => 5,
            _ => 3
        };

        if (quality < 3)
        {
            state.Lapses++;
            state.Streak = 0;
            state.IntervalDays = 0;
            state.EaseFactor = Math.Max(1.3, state.EaseFactor - 0.2);
            state.DueAt = now;
            return;
        }

        state.Streak++;
        if (state.Streak == 1)
        {
            state.IntervalDays = 1;
        }
        else if (state.Streak == 2)
        {
            state.IntervalDays = 3;
        }
        else
        {
            state.IntervalDays = Math.Max(1, (int)Math.Round(state.IntervalDays * state.EaseFactor));
        }

        if (rating == CardReviewRating.Easy && state.IntervalDays < easyMinInterval)
        {
            state.IntervalDays = easyMinInterval;
        }

        if (state.IntervalDays > maxInterval)
        {
            state.IntervalDays = maxInterval;
        }

        var delta = 0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02);
        state.EaseFactor = Math.Max(1.3, state.EaseFactor + delta);
        state.DueAt = now.AddDays(state.IntervalDays);
    }
}
