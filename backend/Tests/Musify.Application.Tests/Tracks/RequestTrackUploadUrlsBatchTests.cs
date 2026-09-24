using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Services;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class RequestTrackUploadUrlsBatchTests : HandlerTestBase
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private readonly IStorageService _storageService = Substitute.For<IStorageService>();

        private RequestTrackUploadUrlsCommandHandler CreateHandler()
        {
            _storageService
                .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(call => $"https://storage.musify.test/{call.ArgAt<string>(1)}");

            var config = TestConfigurations.UploadIntent();
            return new RequestTrackUploadUrlsCommandHandler(
                new UploadIntentIssuer(
                    Database,
                    _storageService,
                    new UploadIntentValidator(Database, _storageService, config),
                    TestConfigurations.Storage(),
                    config,
                    NoOpLogger<UploadIntentIssuer>()),
                TestConfigurations.Storage(),
                TestConfigurations.Track(),
                config);
        }

        private static readonly AudioUploadSpec[] _threeAudios =
        [
            new("mp3", "audio/mpeg", 3_000_000),
            new("flac", "audio/flac", 30_000_000),
            new("wav", "audio/wav", 40_000_000)
        ];

        [Fact]
        public async Task Handle_ManyAudiosAndSharedPicture_IssuesOneAudioIntentPerFileAndOnePictureIntentPerTrackOnASingleObject()
        {
            var user = TestEntities.User();
            await SeedAsync(user);

            var result = await CreateHandler().Handle(
                new RequestTrackUploadUrlsCommand(user.Id, "webp", "image/webp", Audios: _threeAudios), Ct);

            Assert.False(result.IsError);
            Assert.Equal(3, result.Value.Audios.Count);
            Assert.All(result.Value.Audios, audio => Assert.NotNull(audio.PictureIntentId));

            var intents = await Database.UploadIntents.AsNoTracking().ToListAsync(Ct);
            var pictures = intents.Where(intent => intent.Purpose == UploadIntentPurpose.TrackPicture).ToList();
            var audios = intents.Where(intent => intent.Purpose == UploadIntentPurpose.TrackAudio).ToList();
            Assert.Equal(3, pictures.Count);
            Assert.Equal(3, audios.Count);

            Assert.Single(pictures.Select(intent => intent.Key).Distinct());
            Assert.Equal(3, pictures.Select(intent => intent.ObjectName).Distinct().Count());
            Assert.Equal(3, audios.Select(intent => intent.Key).Distinct().Count());
            Assert.Equal(
                result.Value.Audios.Select(audio => audio.PictureIntentId!.Value).Order(),
                pictures.Select(intent => intent.Id).Order());
            Assert.Equal(result.Value.Audios.Select(audio => audio.IntentId).Order(), audios.Select(intent => intent.Id).Order());
            Assert.Equal(result.Value.Audios[0].IntentId, result.Value.AudioIntentId);
            Assert.Equal(result.Value.Audios[0].PictureIntentId, result.Value.PictureIntentId);
            Assert.Equal(pictures[0].Key, result.Value.PictureKey);
        }

        [Fact]
        public async Task Handle_ManyAudiosWithoutPicture_IssuesNoPictureIntents()
        {
            var user = TestEntities.User();
            await SeedAsync(user);

            var result = await CreateHandler().Handle(
                new RequestTrackUploadUrlsCommand(user.Id, null, null, Audios: _threeAudios), Ct);

            Assert.False(result.IsError);
            Assert.Null(result.Value.PictureIntentId);
            Assert.Null(result.Value.PictureUploadUrl);
            Assert.All(result.Value.Audios, audio => Assert.Null(audio.PictureIntentId));
            Assert.All(await Database.UploadIntents.ToListAsync(Ct), intent => Assert.Equal(UploadIntentPurpose.TrackAudio, intent.Purpose));
        }

        [Fact]
        public async Task Handle_LegacyAudioFields_StillReturnASingleItem()
        {
            var user = TestEntities.User();
            await SeedAsync(user);

            var result = await CreateHandler().Handle(new RequestTrackUploadUrlsCommand(user.Id, "webp", "image/webp", "mp3", "audio/mpeg"), Ct);

            Assert.False(result.IsError);
            var item = Assert.Single(result.Value.Audios);
            Assert.Equal(result.Value.AudioIntentId, item.IntentId);
            Assert.Equal(result.Value.PictureIntentId, item.PictureIntentId);
        }

        [Fact]
        public async Task Handle_NoAudio_ReturnsValidationError()
        {
            var user = TestEntities.User();
            await SeedAsync(user);

            var result = await CreateHandler().Handle(new RequestTrackUploadUrlsCommand(user.Id, "webp", "image/webp"), Ct);

            Assert.Equal(ErrorType.Validation, result.FirstError.Type);
            Assert.Empty(await Database.UploadIntents.ToListAsync(Ct));
        }
    }
}
