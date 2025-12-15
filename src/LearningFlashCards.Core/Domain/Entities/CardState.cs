namespace LearningFlashCards.Core.Domain.Entities;

public class CardState
{
    public DateTimeOffset DueAt { get; set; } = DateTimeOffset.UtcNow;
    public int IntervalDays { get; set; }
    public double EaseFactor { get; set; } = 2.5;
    public int Streak { get; set; }
    public int Lapses { get; set; }
}
