using Musify.Application.Albums;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class GetAlbumsQueryHandlerTests : HandlerTestBase
    {
        private GetAlbumsQueryHandler CreateHandler() => new(Database);

        [Fact]
        public async Task Handle_NoTitleFilter_ReturnsAllAlbumsPaged()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.UserAlbum(owner.Id, "Alpha"), TestEntities.UserAlbum(owner.Id, "Beta"));

            var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: null, PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.TotalItemCount);
        }

        [Fact]
        public async Task Handle_TitleFilter_ReturnsOnlyMatchingAlbums()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.UserAlbum(owner.Id, "Rock Classics"), TestEntities.UserAlbum(owner.Id, "Jazz Favorites"));

            var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: "rock", PageNumber: 1, PageSize: 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var album = Assert.Single(result.Value.Items);
            Assert.Equal("Rock Classics", album.Title);
        }

        [Fact]
        public async Task Handle_PageSizeSmallerThanTotal_ReportsHasNextPage()
        {
            var owner = TestEntities.User();
            await SeedAsync(owner, TestEntities.UserAlbum(owner.Id, "One"), TestEntities.UserAlbum(owner.Id, "Two"), TestEntities.UserAlbum(owner.Id, "Three"));

            var result = await CreateHandler().Handle(new GetAlbumsQuery(Title: null, PageNumber: 1, PageSize: 2), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(2, result.Value.Items.Count);
            Assert.True(result.Value.HasNextPage);
        }
    }
}
