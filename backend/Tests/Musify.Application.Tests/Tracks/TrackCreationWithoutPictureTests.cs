using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Pictures;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class TrackCreationWithoutPictureTests : HandlerTestBase
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
        private readonly IStorageService _storageService = Substitute.For<IStorageService>();

        private CreateTrackCommandHandler CreateHandler()
        {
            _storageService
                .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ObjectMetaData("audio/mpeg", 1024));

            return new CreateTrackCommandHandler(
                Database,
                _eventBus,
                new UploadIntentValidator(Database, _storageService, TestConfigurations.UploadIntent()),
                NoOpLogger<CreateTrackCommandHandler>(),
                TestConfigurations.Track());
        }

        [Fact]
        public async Task CreateTrack_WithoutPicture_UsesPresetsAndSkipsPictureProcessing()
        {
            var user = TestEntities.User();
            var audio = TestEntities.UploadIntent(user.Id, UploadIntentPurpose.TrackAudio, objectName: "song.mp3");
            await SeedAsync(user, audio);
            var routes = TestConfigurations.Track().Routes;

            var result = await CreateHandler().Handle(new CreateTrackCommand(user.Id, "No cover", null, audio.Id, [Genre.Pop]), Ct);

            Assert.False(result.IsError);
            Assert.False(result.Value.IsPublished);
            var stored = await Database.Tracks.AsNoTracking().SingleAsync(Ct);
            Assert.Null(stored.Pictures.OriginalName);
            Assert.Equal(routes.PresetSmallPicture, stored.Pictures.SmallName);
            Assert.Equal(routes.PresetMediumPicture, stored.Pictures.MediumName);
            Assert.Equal(routes.PresetLargePicture, stored.Pictures.LargeName);
            Assert.Equal(ProcessingStatus.Completed, stored.Pictures.ProcessingStatus);
            Assert.False(stored.IsPublished);

            await _eventBus.Received(1).PublishAsync(
                Arg.Is<CreateTrackResourcesEvent>(message =>
                    message.PictureIntentId == null
                    && message.PictureSourceKey == null
                    && message.PictureDestinationKey == null
                    && message.AudioIntentId == audio.Id),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateTrack_WithPicture_StaysUnpublishedAndKeepsPictureFlow()
        {
            var user = TestEntities.User();
            var picture = TestEntities.UploadIntent(user.Id, UploadIntentPurpose.TrackPicture, objectName: "cover.webp");
            var audio = TestEntities.UploadIntent(user.Id, UploadIntentPurpose.TrackAudio, objectName: "song.mp3");
            await SeedAsync(user, picture, audio);

            var result = await CreateHandler().Handle(new CreateTrackCommand(user.Id, "Cover", picture.Id, audio.Id, [Genre.Pop]), Ct);

            Assert.False(result.IsError);
            Assert.False(result.Value.IsPublished);
            var stored = await Database.Tracks.AsNoTracking().SingleAsync(Ct);
            Assert.Equal("cover.webp", stored.Pictures.OriginalName);
            Assert.Equal(ProcessingStatus.Pending, stored.Pictures.ProcessingStatus);
            await _eventBus.Received(1).PublishAsync(
                Arg.Is<CreateTrackResourcesEvent>(message => message.PictureIntentId == picture.Id && message.PictureSourceKey != null),
                Arg.Any<CancellationToken>());
        }

        private GetCoverQueryHandler CreateCoverHandler() => new(
            Database,
            TestConfigurations.Storage("covers-bucket"),
            TestConfigurations.Track(),
            TestConfigurations.Album(),
            TestConfigurations.PlayList());

        [Fact]
        public async Task Cover_TrackWithoutPictureInAnAlbum_UsesTheAlbumCover()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner, pictures: new TrackPictures
            {
                SmallName = "PresetSmallPicture.webp",
                MediumName = "PresetMediumPicture.webp",
                LargeName = "PresetLargePicture.webp",
                ProcessingStatus = ProcessingStatus.Completed
            });
            var album = TestEntities.Album(owner.Id, pictures: new EntityPictures
            {
                OriginalName = "album-original.webp",
                SmallName = "album-small.webp",
                MediumName = "album-medium.webp",
                LargeName = "album-large.webp"
            });
            await SeedAsync(owner, track, album, new AlbumHasTrack { AlbumId = album.Id, TrackId = track.Id, TrackNumber = 1 });

            var result = await CreateCoverHandler().Handle(new GetCoverQuery(CoverOwner.Track, track.Id, PictureSize.Small), Ct);

            Assert.False(result.IsError);
            Assert.EndsWith("album-small.webp", result.Value.Key);
        }

        [Fact]
        public async Task Cover_TrackWithoutPictureAndAlbum_FallsBackToThePreset()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner, pictures: new TrackPictures
            {
                SmallName = "PresetSmallPicture.webp",
                MediumName = "PresetMediumPicture.webp",
                LargeName = "PresetLargePicture.webp",
                ProcessingStatus = ProcessingStatus.Completed
            });
            await SeedAsync(owner, track);

            var result = await CreateCoverHandler().Handle(new GetCoverQuery(CoverOwner.Track, track.Id, PictureSize.Small), Ct);

            Assert.False(result.IsError);
            Assert.EndsWith("PresetSmallPicture.webp", result.Value.Key);
        }

        [Fact]
        public async Task Cover_TrackWithOwnPicture_IgnoresTheAlbumCover()
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, track, album, new AlbumHasTrack { AlbumId = album.Id, TrackId = track.Id, TrackNumber = 1 });

            var result = await CreateCoverHandler().Handle(new GetCoverQuery(CoverOwner.Track, track.Id, PictureSize.Small), Ct);

            Assert.EndsWith("small.webp", result.Value.Key);
            Assert.DoesNotContain("album", result.Value.Key, StringComparison.OrdinalIgnoreCase);
        }
    }
}
