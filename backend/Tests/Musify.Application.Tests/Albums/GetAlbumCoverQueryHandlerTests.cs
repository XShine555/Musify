using ErrorOr;
using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class GetAlbumCoverQueryHandlerTests : HandlerTestBase
    {
        private GetAlbumCoverQueryHandler CreateHandler() =>
            new(Database, TestConfigurations.Storage("covers-bucket"), TestConfigurations.Album());

        [Theory]
        [InlineData("small", "small.webp")]
        [InlineData("large", "large.webp")]
        [InlineData("medium", "medium.webp")]
        public async Task Handle_ExistingAlbum_ReturnsRequestedSize(string size, string expectedFileName)
        {
            var owner = TestEntities.User();
            var album = TestEntities.UserAlbum(owner.Id);
            await SeedAsync(owner, album);

            var result = await CreateHandler().Handle(new GetAlbumCoverQuery(album.Id, size), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("covers-bucket", result.Value.Bucket);
            Assert.EndsWith(expectedFileName, result.Value.Key);
        }

        [Fact]
        public async Task Handle_AlbumMissing_ReturnsNotFound()
        {
            var result = await CreateHandler().Handle(new GetAlbumCoverQuery(Guid.NewGuid(), "small"), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }
    }
}
