using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Albums
{
    public sealed class RequestAlbumPictureUploadCommandHandlerTests : HandlerTestBase
    {
        private readonly IStorageService storageService = Substitute.For<IStorageService>();

        private RequestAlbumPictureUploadCommandHandler CreateHandler(UploadIntentConfiguration? uploadIntentConfig = null)
        {
            storageService
                .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns("https://storage.musify.test/presigned-upload");

            var config = uploadIntentConfig ?? TestConfigurations.UploadIntent();

            return new RequestAlbumPictureUploadCommandHandler(
                new UploadIntentIssuer(
                    Database,
                    storageService,
                    new UploadIntentValidator(Database, storageService, config),
                    TestConfigurations.Storage(),
                    config,
                    NoOpLogger<UploadIntentIssuer>()),
                TestConfigurations.Album());
        }

        [Fact]
        public async Task Handle_UserExists_IssuesUploadUrlAndCreatesIntent()
        {
            var user = TestEntities.User();
            await SeedAsync(user);

            var command = new RequestAlbumPictureUploadCommand(user.Id, "webp", "image/webp");

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            Assert.Equal("https://storage.musify.test/presigned-upload", result.Value.UploadUrl);
            Assert.Single(await Database.UploadIntents.ToListAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_UserMissing_ReturnsNotFound()
        {
            var command = new RequestAlbumPictureUploadCommand(404, "webp", "image/webp");

            var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Fact]
        public async Task Handle_UserOverByteQuota_ReturnsValidationErrorAndCreatesNoIntent()
        {
            var config = TestConfigurations.UploadIntent();
            config.MaxActiveUploadBytesPerUser = 100;

            var user = TestEntities.User();
            await SeedAsync(user);

            var command = new RequestAlbumPictureUploadCommand(user.Id, "webp", "image/webp", ExpectedSizeBytes: 1_000_000);

            var result = await CreateHandler(config).Handle(command, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Validation, result.FirstError.Type);
            Assert.Empty(await Database.UploadIntents.ToListAsync(TestContext.Current.CancellationToken));
        }
    }
}
