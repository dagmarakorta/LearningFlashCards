using LearningFlashCards.Core.Domain.Sync;

namespace LearningFlashCards.Core.Contracts.Sync;

public class SyncEnvelope<T>
{
    public string? SinceToken { get; set; }
    public string? NextToken { get; set; }
    public IList<SyncChange<T>> Changes { get; set; } = new List<SyncChange<T>>();
}
