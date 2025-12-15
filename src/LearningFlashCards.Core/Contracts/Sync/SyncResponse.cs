namespace LearningFlashCards.Core.Contracts.Sync;

public class SyncResponse<T>
{
    public string? NextToken { get; set; }
    public IList<SyncChangeDto<T>> Changes { get; set; } = new List<SyncChangeDto<T>>();
}
