using ErrorOr;
using Musify.Application.Configuration;
using Musify.Application.Pictures;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Application.Tests.Pictures
{
    public sealed class GetCoverQueryHandlerTests : HandlerTestBase
    {
        private GetCoverQueryHandler CreateHandler() => new(
            Database,
            TestConfigurations.Storage("covers-bucket"),
            TestConfigurations.Track(),
            TestConfigurations.Album(),
            TestConfigurations.PlayList());

        [Theory]
        [InlineData(PictureSize.Small, "small.webp")]
        [InlineData(PictureSize.Medium, "medium.webp")]
        [InlineData(PictureSize.Large, "large.webp")]
        public async Task Handle_ProcessedTrack_ReturnsRequestedSize(PictureSize size, string expectedFileName)
        {
            var owner = TestEntities.User();
            var track = TestEntities.Track(owner);
            await SeedAsync(owner, track);

            var result = await CreateHandler().Handle(new GetCoverQuery(CoverOwner.Track, track.Id, size), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("covers-bucket", result.Value.Bucket);
            Assert.EndsWith(expectedFileName, result.Value.Key);
            Assert.Equal("image/webp", result.Value.ContentType);
        }

        [Theory]
        [InlineData(PictureSize.Small, "small.webp")]
        [InlineData(PictureSize.Medium, "medium.webp")]
        [InlineData(PictureSize.Large, "large.webp")]
        public async Task Handle_ProcessedAlbum_ReturnsRequestedSize(PictureSize size, string expectedFileName)
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id);
            await SeedAsync(owner, album);

            var result = await CreateHandler().Handle(new GetCoverQuery(CoverOwner.Album, album.Id, size), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.EndsWith(expectedFileName, result.Value.Key);
        }

        [Fact]
        public async Task Handle_ProcessedPlayList_ReturnsRequestedSize()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id);
            await SeedAsync(owner, playList);

            var result = await CreateHandler().Handle(new GetCoverQuery(CoverOwner.PlayList, playList.Id, PictureSize.Large), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.EndsWith("large.webp", result.Value.Key);
        }

        [Theory]
        [InlineData(CoverOwner.Track)]
        [InlineData(CoverOwner.Album)]
        [InlineData(CoverOwner.PlayList)]
        public async Task Handle_EntityMissing_ReturnsNotFound(CoverOwner owner)
        {
            var result = await CreateHandler().Handle(new GetCoverQuery(owner, Guid.NewGuid()), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_AlbumPicturesStillProcessing_FallsBackToTheOriginal()
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id, pictures: EntityPictures.Pending("original.png"));
            await SeedAsync(owner, album);

            var result = await CreateHandler().Handle(new GetCoverQuery(CoverOwner.Album, album.Id, PictureSize.Small), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Contains($"{owner.Id}", result.Value.Key);
            Assert.EndsWith("original.png", result.Value.Key);
            Assert.Equal("image/png", result.Value.ContentType);
        }

        [Fact]
        public async Task Handle_RemovingAlbum_ReturnsNotFound()
        {
            var owner = TestEntities.User();
            var album = TestEntities.Album(owner.Id);
            album.LifeCycleStatus = LifeCycleStatus.Removing;
            await SeedAsync(owner, album);

            var result = await CreateHandler().Handle(new GetCoverQuery(CoverOwner.Album, album.Id), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
