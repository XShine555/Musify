using ErrorOr;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Services;

public sealed class UploadIntentValidatorTests : HandlerTestBase
{
    private readonly IStorageService storageService = Substitute.For<IStorageService>();
    private readonly Domain.Entities.User owner = TestEntities.User();

    private UploadIntentValidator CreateValidator() => new(Database, storageService);

    [Fact]
    public async Task ValidateAndLoadAsync_UploadedObjectWithinLimits_ReturnsTheIntent()
    {
        storageService
            .HeadObjectAsync("bucket", "temp/object.webp", Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 2048));

        var intent = TestEntities.UploadIntent(owner.Id, bucket: "bucket", key: "temp/object.webp");
        await SeedAsync(owner, intent);

        var result = await CreateValidator().ValidateAndLoadAsync(TestConfigurations.UploadIntent(), intent.Id, owner.Id, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(intent.Id, result.Value.Id);
    }

    [Fact]
    public async Task ValidateAndLoadAsync_IntentMissing_ReturnsNotFound()
    {
        var result = await CreateValidator().ValidateAndLoadAsync(TestConfigurations.UploadIntent(), Guid.NewGuid(), owner.Id, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task ValidateAndLoadAsync_IntentBelongsToAnotherUser_ReturnsNotFound()
    {
        var stranger = TestEntities.User(2, "stranger");
        var intent = TestEntities.UploadIntent(owner.Id);
        await SeedAsync(owner, stranger, intent);

        var result = await CreateValidator().ValidateAndLoadAsync(TestConfigurations.UploadIntent(), intent.Id, stranger.Id, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task ValidateAndLoadAsync_AlreadyConsumed_ReturnsConflict()
    {
        var intent = TestEntities.UploadIntent(owner.Id, status: UploadIntentStatus.Consumed);
        await SeedAsync(owner, intent);

        var result = await CreateValidator().ValidateAndLoadAsync(TestConfigurations.UploadIntent(), intent.Id, owner.Id, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }

    [Fact]
    public async Task ValidateAndLoadAsync_PastExpiry_ReturnsValidationError()
    {
        var intent = TestEntities.UploadIntent(owner.Id, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        await SeedAsync(owner, intent);

        var result = await CreateValidator().ValidateAndLoadAsync(TestConfigurations.UploadIntent(), intent.Id, owner.Id, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task ValidateAndLoadAsync_ObjectNeverUploaded_ReturnsNotFound()
    {
        storageService
            .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ObjectMetaData?)null);

        var intent = TestEntities.UploadIntent(owner.Id);
        await SeedAsync(owner, intent);

        var result = await CreateValidator().ValidateAndLoadAsync(TestConfigurations.UploadIntent(), intent.Id, owner.Id, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task ValidateAndLoadAsync_UploadedObjectExceedsMaxSize_ReturnsValidationError()
    {
        var config = TestConfigurations.UploadIntent();
        config.MaxUploadBytes = 1024;

        storageService
            .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 2048));

        var intent = TestEntities.UploadIntent(owner.Id);
        await SeedAsync(owner, intent);

        var result = await CreateValidator().ValidateAndLoadAsync(config, intent.Id, owner.Id, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task CheckQuotaAsync_WithinLimits_ReturnsSuccess()
    {
        await SeedAsync(owner);

        var result = await CreateValidator().CheckQuotaAsync(TestConfigurations.UploadIntent(), owner.Id, requiredBytes: 1024, requiredIntentCount: 1, CancellationToken.None);

        Assert.False(result.IsError);
    }

    [Fact]
    public async Task CheckQuotaAsync_TooManyActiveIntents_ReturnsValidationError()
    {
        var config = TestConfigurations.UploadIntent();
        config.MaxActiveUploadIntentsPerUser = 1;
        await SeedAsync(owner, TestEntities.UploadIntent(owner.Id));

        var result = await CreateValidator().CheckQuotaAsync(config, owner.Id, requiredBytes: 1, requiredIntentCount: 1, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task CheckQuotaAsync_ExpiredOrConsumedIntentsDoNotCountTowardsTheLimit()
    {
        var config = TestConfigurations.UploadIntent();
        config.MaxActiveUploadIntentsPerUser = 1;
        await SeedAsync(
            owner,
            TestEntities.UploadIntent(owner.Id, status: UploadIntentStatus.Expired),
            TestEntities.UploadIntent(owner.Id, status: UploadIntentStatus.Consumed));

        var result = await CreateValidator().CheckQuotaAsync(config, owner.Id, requiredBytes: 1, requiredIntentCount: 1, CancellationToken.None);

        Assert.False(result.IsError);
    }
}
