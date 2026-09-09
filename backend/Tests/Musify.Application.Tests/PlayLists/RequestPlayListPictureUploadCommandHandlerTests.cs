using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.PlayLists;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.PlayLists;

public sealed class RequestPlayListPictureUploadCommandHandlerTests : HandlerTestBase
{
    private readonly IStorageService storageService = Substitute.For<IStorageService>();

    private RequestPlayListPictureUploadCommandHandler CreateHandler(UploadIntentConfiguration? uploadIntentConfig = null)
    {
        storageService
            .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.musify.test/presigned-upload");

        return new RequestPlayListPictureUploadCommandHandler(
            Database,
            new UploadIntentValidator(Database, storageService),
            storageService,
            NoOpLogger<RequestPlayListPictureUploadCommandHandler>(),
            TestConfigurations.Storage(),
            TestConfigurations.PlayList(),
            uploadIntentConfig ?? TestConfigurations.UploadIntent());
    }

    [Fact]
    public async Task Handle_UserExists_IssuesUploadUrlAndCreatesIntent()
    {
        var user = TestEntities.User();
        await SeedAsync(user);

        var command = new RequestPlayListPictureUploadCommand(user.Id, "webp", "image/webp");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("https://storage.musify.test/presigned-upload", result.Value.UploadUrl);
        Assert.Single(await Database.UploadIntents.ToListAsync());
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsNotFound()
    {
        var command = new RequestPlayListPictureUploadCommand(404, "webp", "image/webp");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_UserOverByteQuota_ReturnsValidationErrorAndCreatesNoIntent()
    {
        var config = TestConfigurations.UploadIntent();
        config.MaxActiveUploadBytesPerUser = 100;

        var user = TestEntities.User();
        await SeedAsync(user);

        var command = new RequestPlayListPictureUploadCommand(user.Id, "webp", "image/webp", ExpectedSizeBytes: 1_000_000);

        var result = await CreateHandler(config).Handle(command, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Empty(await Database.UploadIntents.ToListAsync());
    }
}
