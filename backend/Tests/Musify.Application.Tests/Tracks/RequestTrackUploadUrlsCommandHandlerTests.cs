using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks;

public sealed class RequestTrackUploadUrlsCommandHandlerTests : HandlerTestBase
{
    private readonly IStorageService storageService = Substitute.For<IStorageService>();

    private RequestTrackUploadUrlsCommandHandler CreateHandler(UploadIntentConfiguration? uploadIntentConfig = null)
    {
        storageService
            .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.musify.test/presigned-upload");

        var config = uploadIntentConfig ?? TestConfigurations.UploadIntent();

        return new RequestTrackUploadUrlsCommandHandler(
            new UploadIntentIssuer(
                Database,
                storageService,
                new UploadIntentValidator(Database, storageService, config),
                TestConfigurations.Storage(),
                config,
                NoOpLogger<UploadIntentIssuer>()),
            TestConfigurations.Storage(),
            TestConfigurations.Track(),
            config);
    }

    [Fact]
    public async Task Handle_UserExists_IssuesBothUploadUrlsAndCreatesIntents()
    {
        var user = TestEntities.User();
        await SeedAsync(user);

        var command = new RequestTrackUploadUrlsCommand(user.Id, "webp", "image/webp", "mp3", "audio/mpeg");

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("https://storage.musify.test/presigned-upload", result.Value.PictureUploadUrl);
        Assert.Equal("https://storage.musify.test/presigned-upload", result.Value.AudioUploadUrl);

        var intents = await Database.UploadIntents.Where(intent => intent.UserId == user.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, intents.Count);
        Assert.Contains(intents, intent => intent.Purpose == UploadIntentPurpose.TrackPicture);
        Assert.Contains(intents, intent => intent.Purpose == UploadIntentPurpose.TrackAudio);
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsNotFound()
    {
        var command = new RequestTrackUploadUrlsCommand(404, "webp", "image/webp", "mp3", "audio/mpeg");

        var result = await CreateHandler().Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_UserAtActiveIntentLimit_ReturnsValidationErrorAndCreatesNothing()
    {
        var config = TestConfigurations.UploadIntent();
        config.MaxActiveUploadIntentsPerUser = 1;

        var user = TestEntities.User();
        await SeedAsync(user, TestEntities.UploadIntent(user.Id));

        var command = new RequestTrackUploadUrlsCommand(user.Id, "webp", "image/webp", "mp3", "audio/mpeg");

        var result = await CreateHandler(config).Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal(1, await Database.UploadIntents.CountAsync(TestContext.Current.CancellationToken));
    }
}
