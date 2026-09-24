using ErrorOr;
using Musify.Application.PlayLists;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.PlayLists
{
    public sealed class UpdatePlayListCommandHandlerTests : HandlerTestBase
    {
        private UpdatePlayListCommandHandler CreateHandler() => new(
            Database,
            NoOpLogger<UpdatePlayListCommandHandler>());

        [Fact]
        public async Task Handle_Owner_RenamesPlayList()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id, "Old Name");
            await SeedAsync(owner, playList);

            var command = new UpdatePlayListCommand(owner.Id, playList.Id, "New Name", null, Musify.Domain.ValueObjects.PlayListVisibility.Private);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("New Name", result.Value.Name);
        }

        [Fact]
        public async Task Handle_PlayListMissing_ReturnsNotFound()
        {
            var command = new UpdatePlayListCommand(1, Guid.NewGuid(), "New Name", null, Musify.Domain.ValueObjects.PlayListVisibility.Private);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NotOwner_ReturnsForbidden()
        {
            var owner = TestEntities.User(1, "owner");
            var stranger = TestEntities.User(2, "stranger");
            var playList = TestEntities.PlayList(owner.Id);
            await SeedAsync(owner, stranger, playList);

            var command = new UpdatePlayListCommand(stranger.Id, playList.Id, "Hijacked", null, Musify.Domain.ValueObjects.PlayListVisibility.Private);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_NewVisibility_UpdatesIt()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id);
            await SeedAsync(owner, playList);

            var command = new UpdatePlayListCommand(owner.Id, playList.Id, "Same Name", null, Musify.Domain.ValueObjects.PlayListVisibility.Public);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal(Musify.Domain.ValueObjects.PlayListVisibility.Public, result.Value.Visibility);
        }

        [Fact]
        public async Task Handle_ReplacesNameDescriptionAndVisibility()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id, "Original Name", "Original description", visibility: Musify.Domain.ValueObjects.PlayListVisibility.Public);
            await SeedAsync(owner, playList);

            var command = new UpdatePlayListCommand(owner.Id, playList.Id, "  New Name  ", "New description", Musify.Domain.ValueObjects.PlayListVisibility.Private);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("New Name", result.Value.Name);
            Assert.Equal("New description", result.Value.Description);
            Assert.Equal(Musify.Domain.ValueObjects.PlayListVisibility.Private, result.Value.Visibility);
        }

        [Fact]
        public async Task Handle_MissingDescription_ClearsIt()
        {
            var owner = TestEntities.User();
            var playList = TestEntities.PlayList(owner.Id, "Original Name", "Original description");
            await SeedAsync(owner, playList);

            var command = new UpdatePlayListCommand(owner.Id, playList.Id, "Original Name", null, Musify.Domain.ValueObjects.PlayListVisibility.Private);

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Null(result.Value.Description);
        }
    }
}
