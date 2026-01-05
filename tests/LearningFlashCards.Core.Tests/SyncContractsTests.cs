using LearningFlashCards.Core.Contracts.Sync;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;

namespace LearningFlashCards.Core.Tests;

public class SyncContractsTests
{
    [Fact]
    public void SyncRequest_Defaults_InitializeCollections()
    {
        var request = new SyncRequest<Card>();

        Assert.NotNull(request.Changes);
        Assert.Empty(request.Changes);
        Assert.Null(request.SinceToken);
    }

    [Fact]
    public void SyncEnvelope_Defaults_InitializeCollections()
    {
        var envelope = new SyncEnvelope<Tag>();

        Assert.NotNull(envelope.Changes);
        Assert.Empty(envelope.Changes);
        Assert.Null(envelope.SinceToken);
        Assert.Null(envelope.NextToken);
    }

    [Fact]
    public void SyncChange_StoresValues()
    {
        var deck = new Deck { Id = Guid.NewGuid(), Name = "Deck" };

        var change = new SyncChange<Deck>(SyncOperation.Delete, deck, "etag");

        Assert.Equal(SyncOperation.Delete, change.Operation);
        Assert.Equal(deck, change.Entity);
        Assert.Equal("etag", change.ETag);
    }
}
