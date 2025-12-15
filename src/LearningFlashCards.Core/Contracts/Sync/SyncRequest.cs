namespace LearningFlashCards.Core.Contracts.Sync;

public class SyncRequest<T>
{
    public string? SinceToken { get; set; }
    public IList<SyncChangeDto<T>> Changes { get; set; } = new List<SyncChangeDto<T>>();
}
