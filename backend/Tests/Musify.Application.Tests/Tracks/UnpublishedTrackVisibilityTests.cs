using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums;
using Musify.Application.Contracts;
using Musify.Application.Genres;
using Musify.Application.Likes;
using Musify.Application.Mixes;
using Musify.Application.PlayLists;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class UnpublishedTrackVisibilityTests : HandlerTestBase
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private async Task<(User Owner, User Other, Track Live, Track Hidden)> SeedTracksAsync()
        {
            var owner = TestEntities.User(1, "owner");
            var other = TestEntities.User(2, "other");
            var live = TestEntities.Track(owner, "Live");
            var hidden = TestEntities.Track(owner, "Hidden", isPublished: false, tags: [Genre.Jazz]);
            await SeedAsync(owner, other, live, hidden);
            return (owner, other, live, hidden);
        }

        [Fact]
        public async Task Search_HidesUnpublishedTracksEvenFromTheOwner()
        {
            var (_, _, live, _) = await SeedTracksAsync();

            var result = await new GetTracksQueryHandler(Database).Handle(new GetTracksQuery(null, 1, 20), Ct);

            Assert.Equal([live.Id], result.Items.Select(t => t.Id));
        }

        [Fact]
        public async Task Search_ByGenre_HidesUnpublishedTracks()
        {
            await SeedTracksAsync();

            var result = await new GetTracksQueryHandler(Database).Handle(new GetTracksQuery(null, 1, 20, Genre.Jazz), Ct);

            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task UserTracks_OwnerSeesUnpublishedAndCanFilterThem()
        {
            var (owner, _, live, hidden) = await SeedTracksAsync();
            var handler = new GetTracksByUserIdQueryHandler(Database);

            var all = await handler.Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 20, ViewerId: owner.Id), Ct);
            var onlyHidden = await handler.Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 20, ViewerId: owner.Id, IsPublished: false), Ct);
            var onlyLive = await handler.Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 20, ViewerId: owner.Id, IsPublished: true), Ct);

            Assert.Equal(2, all.Value.TotalItemCount);
            Assert.Equal([hidden.Id], onlyHidden.Value.Items.Select(t => t.Id));
            Assert.False(onlyHidden.Value.Items[0].IsPublished == true);
            Assert.Equal([live.Id], onlyLive.Value.Items.Select(t => t.Id));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UserTracks_OtherViewersAndAnonymousOnlySeePublished(bool signedIn)
        {
            var (owner, other, live, _) = await SeedTracksAsync();

            var result = await new GetTracksByUserIdQueryHandler(Database)
                .Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 20, ViewerId: signedIn ? other.Id : null, IsPublished: false), Ct);

            Assert.Empty(result.Value.Items);

            var all = await new GetTracksByUserIdQueryHandler(Database)
                .Handle(new GetTracksByUserIdQuery(owner.Id, null, 1, 20, ViewerId: signedIn ? other.Id : null), Ct);
            Assert.Equal([live.Id], all.Value.Items.Select(t => t.Id));
        }

        [Fact]
        public async Task TrackById_OnlyTheOwnerCanReadAnUnpublishedTrack()
        {
            var (owner, other, _, hidden) = await SeedTracksAsync();
            var handler = new GetTrackByIdQueryHandler(Database);

            var asOwner = await handler.Handle(new GetTrackByIdQuery(hidden.Id, owner.Id), Ct);
            var asOther = await handler.Handle(new GetTrackByIdQuery(hidden.Id, other.Id), Ct);
            var anonymous = await handler.Handle(new GetTrackByIdQuery(hidden.Id), Ct);

            Assert.False(asOwner.IsError);
            Assert.False(asOwner.Value.IsPublished);
            Assert.Equal(ErrorType.NotFound, asOther.FirstError.Type);
            Assert.Equal(ErrorType.NotFound, anonymous.FirstError.Type);
        }

        [Fact]
        public async Task StartListening_OnlyTheOwnerCanPlayAnUnpublishedTrack()
        {
            var (owner, other, _, hidden) = await SeedTracksAsync();
            var ticketService = Substitute.For<IStreamTicketService>();
            ticketService.IssueTicket(Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<long?>()).Returns(new StreamTicket("ticket", 60));
            var playback = TestConfigurations.Playback(allowAnonymousListening: true);
            var issuer = new TrackStreamIssuer(ticketService, TestConfigurations.Track(), TestConfigurations.StreamGateway(), playback);
            var handler = new StartListeningCommandHandler(Database, issuer, playback, NoOpLogger<StartListeningCommandHandler>());

            var asOwner = await handler.Handle(new StartListeningCommand(hidden.Id, owner.Id), Ct);
            var asOther = await handler.Handle(new StartListeningCommand(hidden.Id, other.Id), Ct);
            var anonymous = await handler.Handle(new StartListeningCommand(hidden.Id, null), Ct);

            Assert.False(asOwner.IsError);
            Assert.Equal(ErrorType.NotFound, asOther.FirstError.Type);
            Assert.Equal(ErrorType.NotFound, anonymous.FirstError.Type);
            Assert.Single(await Database.ListeningHistories.ToListAsync(Ct));
        }

        [Fact]
        public async Task Genres_CountOnlyPublishedTracks()
        {
            await SeedTracksAsync();

            var genres = await new GetGenresQueryHandler(Database).Handle(new GetGenresQuery(), Ct);

            var pop = Assert.Single(genres);
            Assert.Equal(Genre.Pop, pop.Genre);
            Assert.Equal(1, pop.TrackCount);
        }

        [Fact]
        public async Task ListeningHistory_AndLastListened_SkipUnpublishedTracks()
        {
            var (owner, _, live, hidden) = await SeedTracksAsync();
            await SeedAsync(
                TestEntities.ListeningHistory(owner.Id, live.Id, DateTime.UtcNow.AddMinutes(-10)),
                TestEntities.ListeningHistory(owner.Id, hidden.Id, DateTime.UtcNow));

            var history = await new GetListeningHistoryQueryHandler(Database).Handle(new GetListeningHistoryQuery(owner.Id), Ct);
            var last = await new GetLastTrackListenedByUserIdQueryHandler(Database).Handle(new GetLastTrackListenedByUserIdQuery(owner.Id), Ct);

            Assert.Equal([live.Id], history.Select(t => t.Id));
            Assert.Equal(live.Id, last?.Id);
        }

        [Fact]
        public async Task Likes_UnpublishedTracksCannotBeLikedAndDisappearFromTheLikedList()
        {
            var (owner, other, live, hidden) = await SeedTracksAsync();
            var toggle = new ToggleTrackLikeCommandHandler(Database, NoOpLogger<ToggleTrackLikeCommandHandler>());

            var likeHidden = await toggle.Handle(new ToggleTrackLikeCommand(other.Id, hidden.Id), Ct);
            var likeLive = await toggle.Handle(new ToggleTrackLikeCommand(other.Id, live.Id), Ct);
            await SeedAsync(new TrackLike { UserId = owner.Id, TrackId = hidden.Id });

            var likedByOwner = await new GetLikedTracksQueryHandler(Database).Handle(new GetLikedTracksQuery(owner.Id, 1, 20), Ct);
            var likedByOther = await new GetLikedTracksQueryHandler(Database).Handle(new GetLikedTracksQuery(other.Id, 1, 20), Ct);

            Assert.Equal(ErrorType.NotFound, likeHidden.FirstError.Type);
            Assert.False(likeLive.IsError);
            Assert.Empty(likedByOwner.Items);
            Assert.Equal([live.Id], likedByOther.Items.Select(t => t.Id));
        }

        [Fact]
        public async Task Mixes_OnlyUseAndShowPublishedTracks()
        {
            var (owner, _, live, hidden) = await SeedTracksAsync();

            await new GenerateMixesForUserCommandHandler(Database, TestConfigurations.Mix(), NoOpLogger<GenerateMixesForUserCommandHandler>())
                .Handle(new GenerateMixesForUserCommand(owner.Id), Ct);

            var generated = await Database.MixItems.Select(item => item.TrackId).Distinct().ToListAsync(Ct);
            Assert.Equal([live.Id], generated);

            var mix = TestEntities.Mix(owner.Id, MixKind.Discovery, position: 5);
            await SeedAsync(mix, TestEntities.MixItem(mix.Id, live.Id, 0), TestEntities.MixItem(mix.Id, hidden.Id, 1));

            var byId = await new GetMixByIdQueryHandler(Database).Handle(new GetMixByIdQuery(owner.Id, mix.Id), Ct);
            var list = await new GetMixesByUserIdQueryHandler(Database).Handle(new GetMixesByUserIdQuery(owner.Id), Ct);

            Assert.Equal([live.Id], byId.Value.Items.Select(i => i.TrackId));
            Assert.Equal(1, byId.Value.ItemCount);
            var listed = list.Single(m => m.Id == mix.Id);
            Assert.Equal(1, listed.ItemCount);
            Assert.Equal([live.Id], listed.Items.Select(i => i.TrackId));
        }

        [Fact]
        public async Task Albums_HideUnpublishedTracksEverywhere()
        {
            var (owner, other, live, hidden) = await SeedTracksAsync();
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(
                album,
                new AlbumHasTrack { AlbumId = album.Id, TrackId = hidden.Id, TrackNumber = 1 },
                new AlbumHasTrack { AlbumId = album.Id, TrackId = live.Id, TrackNumber = 2 },
                TestEntities.ListeningHistory(other.Id, hidden.Id));

            var tracks = await new GetAlbumTracksQueryHandler(Database).Handle(new GetAlbumTracksQuery(album.Id, 1, 20), Ct);
            var detail = await new GetAlbumByIdQueryHandler(Database).Handle(new GetAlbumByIdQuery(album.Id), Ct);
            var byUser = await new GetAlbumsByUserIdQueryHandler(Database).Handle(new GetAlbumsByUserIdQuery(owner.Id, 1, 20), Ct);
            var all = await new GetAlbumsQueryHandler(Database).Handle(new GetAlbumsQuery(null, 1, 20), Ct);
            var recent = await new GetRecentlyListenedAlbumsQueryHandler(Database).Handle(new GetRecentlyListenedAlbumsQuery(other.Id, 10), Ct);
            var ids = await new GetAlbumTrackIdsQueryHandler(Database).Handle(new GetAlbumTrackIdsQuery(owner.Id, album.Id), Ct);

            Assert.Equal([live.Id], tracks.Value.Items.Select(t => t.Id));
            Assert.Equal(1, detail.Value.TrackCount);
            Assert.Equal([live.Id], detail.Value.CoverTrackIds);
            Assert.Equal(1, byUser.Value.Items.Single().TrackCount);
            Assert.Equal(1, all.Items.Single().TrackCount);
            Assert.Empty(recent);
            Assert.Equal([hidden.Id, live.Id], ids.Value.TrackIds);
        }

        [Fact]
        public async Task PlayLists_HideUnpublishedTracksAndRejectAddingThem()
        {
            var (owner, other, live, hidden) = await SeedTracksAsync();
            var playList = TestEntities.PlayList(owner.Id, visibility: PlayListVisibility.Public);
            await SeedAsync(
                playList,
                new PlayListHasTrack { PlayListId = playList.Id, TrackId = hidden.Id, Position = 0 },
                new PlayListHasTrack { PlayListId = playList.Id, TrackId = live.Id, Position = 1 });

            var tracks = await new GetPlayListTracksQueryHandler(Database).Handle(new GetPlayListTracksQuery(playList.Id, 1, 20, owner.Id), Ct);
            var detail = await new GetPlayListByIdQueryHandler(Database).Handle(new GetPlayListByIdQuery(playList.Id, other.Id), Ct);
            var add = await new AddTracksToPlayListCommandHandler(Database, NoOpLogger<AddTracksToPlayListCommandHandler>())
                .Handle(new AddTracksToPlayListCommand(owner.Id, playList.Id, [hidden.Id]), Ct);

            Assert.Equal([live.Id], tracks.Value.Items.Select(t => t.Id));
            Assert.Equal(1, detail.Value.TrackCount);
            Assert.Equal(live.DurationSeconds, detail.Value.DurationSeconds);
            Assert.Equal([live.Id], detail.Value.CoverTrackIds);
            Assert.Equal(ErrorType.NotFound, add.FirstError.Type);
        }

        [Fact]
        public async Task AddAlbumToPlayList_SkipsUnpublishedTracks()
        {
            var (owner, _, live, hidden) = await SeedTracksAsync();
            var album = TestEntities.Album(owner.Id);
            var playList = TestEntities.PlayList(owner.Id);
            await SeedAsync(
                album, playList,
                new AlbumHasTrack { AlbumId = album.Id, TrackId = hidden.Id, TrackNumber = 1 },
                new AlbumHasTrack { AlbumId = album.Id, TrackId = live.Id, TrackNumber = 2 });

            var result = await new AddAlbumToPlayListCommandHandler(Database, NoOpLogger<AddAlbumToPlayListCommandHandler>())
                .Handle(new AddAlbumToPlayListCommand(owner.Id, playList.Id, album.Id), Ct);

            Assert.Equal(1, result.Value.AddedCount);
            Assert.Equal([live.Id], await Database.PlayListHasTracks.Select(link => link.TrackId).ToListAsync(Ct));
        }
    }
}
