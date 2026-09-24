using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Tracks;

public sealed class GetTracksQueryHandlerTests : HandlerTestBase
{
    private GetTracksQueryHandler CreateHandler() => new(Database);

    [Fact]
    public async Task Handle_NoNameFilter_ReturnsEveryTrackAUserOwns()
    {
        var owner = TestEntities.User();
        var trackA = TestEntities.Track(owner, "Alpha");
        var trackB = TestEntities.Track(owner, "Beta");
        await SeedAsync(
            owner, trackA, trackB,
            new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = trackA.Id },
            new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = trackB.Id });

        var result = await CreateHandler().Handle(new GetTracksQuery(Name: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.TotalItemCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_NameFilter_ReturnsOnlyMatchingTracks()
    {
        var owner = TestEntities.User();
        var match = TestEntities.Track(owner, "Bohemian Rhapsody");
        var other = TestEntities.Track(owner, "Imagine");
        await SeedAsync(
            owner, match, other,
            new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = match.Id },
            new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = other.Id });

        var result = await CreateHandler().Handle(new GetTracksQuery(Name: "rhapsody", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("Bohemian Rhapsody", item.Track.Title);
    }

    [Fact]
    public async Task Handle_GenreFilter_ReturnsOnlyTracksWithThatTag()
    {
        var owner = TestEntities.User();
        var rock = TestEntities.Track(owner, "Loud", tags: [Genre.Rock, Genre.Indie]);
        var jazz = TestEntities.Track(owner, "Smooth", tags: [Genre.Jazz]);
        await SeedAsync(
            owner, rock, jazz,
            new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = rock.Id },
            new Musify.Domain.Entities.UserHasTrack { UserId = owner.Id, TrackId = jazz.Id });

        var result = await CreateHandler().Handle(new GetTracksQuery(null, 1, 10, Genre.Indie), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("Loud", item.Track.Title);
    }
}
