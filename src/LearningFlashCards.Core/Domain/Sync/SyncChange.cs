using LearningFlashCards.Core.Domain.Entities;

namespace LearningFlashCards.Core.Domain.Sync;

public record SyncChange<T>(
    SyncOperation Operation,
    T Entity,
    string? ETag);
