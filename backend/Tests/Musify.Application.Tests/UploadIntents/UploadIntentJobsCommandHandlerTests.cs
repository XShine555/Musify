using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Tests.TestSupport;
using Musify.Application.UploadIntents;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.UploadIntents
{
    public sealed class UploadIntentJobsCommandHandlerTests : HandlerTestBase
    {
        [Fact]
        public async Task ExpireUploadIntents_OverdueIssuedIntent_IsMarkedExpired()
        {
            var owner = TestEntities.User();
            var overdue = TestEntities.UploadIntent(owner.Id, expiresAt: DateTime.UtcNow.AddMinutes(-5));
            var active = TestEntities.UploadIntent(owner.Id, expiresAt: DateTime.UtcNow.AddMinutes(5));
            await SeedAsync(owner, overdue, active);
            var handler = new ExpireUploadIntentsCommandHandler(
                Database, TestConfigurations.UploadIntent(), NoOpLogger<ExpireUploadIntentsCommandHandler>());

            var changed = await handler.Handle(new ExpireUploadIntentsCommand(), TestContext.Current.CancellationToken);

            Assert.Equal(1, changed);
            var stored = await Database.UploadIntents.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(UploadIntentStatus.Expired, stored.Single(intent => intent.Id == overdue.Id).Status);
            Assert.Equal(UploadIntentStatus.Issued, stored.Single(intent => intent.Id == active.Id).Status);
        }

        [Fact]
        public async Task ExpireUploadIntents_IntentExpiredLongAgo_IsDeleted()
        {
            var owner = TestEntities.User();
            var old = TestEntities.UploadIntent(
                owner.Id, status: UploadIntentStatus.Expired, expiresAt: DateTime.UtcNow.AddDays(-30));
            await SeedAsync(owner, old);
            var handler = new ExpireUploadIntentsCommandHandler(
                Database, TestConfigurations.UploadIntent(), NoOpLogger<ExpireUploadIntentsCommandHandler>());

            await handler.Handle(new ExpireUploadIntentsCommand(), TestContext.Current.CancellationToken);

            Assert.Empty(await Database.UploadIntents.ToListAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteStaleTempUploads_RemovesOnlyObjectsOlderThanTheRetention()
        {
            var storage = Substitute.For<IStorageService>();
            storage.ListObjectsAsync("test-bucket", "temp", Arg.Any<CancellationToken>()).Returns(Objects(
                new StorageObject("temp/old", DateTime.UtcNow.AddDays(-10)),
                new StorageObject("temp/new", DateTime.UtcNow)));
            var handler = new DeleteStaleTempUploadsCommandHandler(
                storage, TestConfigurations.Storage("test-bucket"), TestConfigurations.UploadIntent(),
                NoOpLogger<DeleteStaleTempUploadsCommandHandler>());

            var deleted = await handler.Handle(new DeleteStaleTempUploadsCommand(), TestContext.Current.CancellationToken);

            Assert.Equal(1, deleted);
            await storage.Received(1).RemoveFileAsync("test-bucket", "temp/old", Arg.Any<CancellationToken>());
            await storage.DidNotReceive().RemoveFileAsync("test-bucket", "temp/new", Arg.Any<CancellationToken>());
        }

        private static async IAsyncEnumerable<StorageObject> Objects(params StorageObject[] objects)
        {
            foreach (var item in objects)
                yield return item;

            await Task.CompletedTask;
        }
    }
}
