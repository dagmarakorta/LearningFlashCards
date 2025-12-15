using LearningFlashCards.Core.Domain.Sync;

namespace LearningFlashCards.Core.Contracts.Sync;

public class SyncChangeDto<T>
{
    public SyncOperation Operation { get; set; }
    public T Payload { get; set; } = default!;
    public string? ETag { get; set; }
}
