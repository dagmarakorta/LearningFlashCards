namespace LearningFlashCards.Core.Domain.Entities;

public class DeckStudySettings
{
    public int DailyReviewLimit { get; set; } = 50;
    public int EasyMinIntervalDays { get; set; } = 3;
    public int MaxIntervalDays { get; set; } = 180;
    public bool RepeatInSession { get; set; } = true;
}
