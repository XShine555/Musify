using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.Validators.Albums;
using Musify.Api.Validators.Tracks;
using Musify.Application.Configuration;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Api.Tests.Validators
{
    public sealed class LibraryRequestValidatorTests
    {
        private readonly UploadIntentConfiguration _configuration = new();

        [Fact]
        public void CreateTrack_WithoutPicture_Passes()
        {
            var request = new CreateTrackRequest("Song", null, Guid.NewGuid(), [Genre.Pop]);

            Assert.True(new CreateTrackRequestValidator().Validate(request).IsValid);
        }

        [Fact]
        public void CreateTrack_EmptyPictureIntent_Fails()
        {
            var request = new CreateTrackRequest("Song", Guid.Empty, Guid.NewGuid(), [Genre.Pop]);

            Assert.False(new CreateTrackRequestValidator().Validate(request).IsValid);
        }

        [Fact]
        public void UploadUrls_ManyAudiosWithSharedPicture_Passes()
        {
            var request = new RequestTrackUploadUrlsRequest(
                "png", "image/png", Audios: [new("mp3", "audio/mpeg", 10), new("flac", "audio/flac")]);

            Assert.True(new RequestTrackUploadUrlsRequestValidator(_configuration).Validate(request).IsValid);
        }

        [Fact]
        public void UploadUrls_AudiosWithoutPicture_Passes()
        {
            var request = new RequestTrackUploadUrlsRequest(Audios: [new("mp3", "audio/mpeg")]);

            Assert.True(new RequestTrackUploadUrlsRequestValidator(_configuration).Validate(request).IsValid);
        }

        [Fact]
        public void UploadUrls_NoAudioAtAll_Fails()
        {
            Assert.False(new RequestTrackUploadUrlsRequestValidator(_configuration).Validate(new RequestTrackUploadUrlsRequest("png", "image/png")).IsValid);
        }

        [Fact]
        public void UploadUrls_LegacyAudioTogetherWithAudiosList_Fails()
        {
            var request = new RequestTrackUploadUrlsRequest("png", "image/png", "mp3", "audio/mpeg", Audios: [new("mp3", "audio/mpeg")]);

            Assert.False(new RequestTrackUploadUrlsRequestValidator(_configuration).Validate(request).IsValid);
        }

        [Fact]
        public void UploadUrls_HalfAPicture_Fails()
        {
            var request = new RequestTrackUploadUrlsRequest("png", null, Audios: [new("mp3", "audio/mpeg")]);

            Assert.False(new RequestTrackUploadUrlsRequestValidator(_configuration).Validate(request).IsValid);
        }

        [Fact]
        public void UploadUrls_InvalidAudioInTheList_Fails()
        {
            var request = new RequestTrackUploadUrlsRequest(Audios: [new("mp3", "audio/mpeg"), new("png", "image/png")]);

            Assert.False(new RequestTrackUploadUrlsRequestValidator(_configuration).Validate(request).IsValid);
        }

        [Fact]
        public void UploadUrls_TooManyAudios_Fails()
        {
            var audios = Enumerable.Range(0, RequestTrackUploadUrlsRequestValidator.MaxAudiosPerRequest + 1)
                .Select(_ => new AudioUploadRequest("mp3", "audio/mpeg"))
                .ToList();

            Assert.False(new RequestTrackUploadUrlsRequestValidator(_configuration).Validate(new RequestTrackUploadUrlsRequest(Audios: audios)).IsValid);
        }

        [Fact]
        public void SetTracksPublished_RequiresBetweenOneAndTheMaximumTracks()
        {
            var validator = new SetTracksPublishedRequestValidator();
            var tooMany = Enumerable.Range(0, SetTracksPublishedRequestValidator.MaxTracksPerRequest + 1).Select(_ => Guid.NewGuid()).ToList();

            Assert.True(validator.Validate(new SetTracksPublishedRequest([Guid.NewGuid()], true)).IsValid);
            Assert.False(validator.Validate(new SetTracksPublishedRequest([], true)).IsValid);
            Assert.False(validator.Validate(new SetTracksPublishedRequest(tooMany, true)).IsValid);
            Assert.False(validator.Validate(new SetTracksPublishedRequest([Guid.Empty], true)).IsValid);
        }

        [Fact]
        public void CreateAlbum_UnknownTypeOrTooManyTracks_Fails()
        {
            var validator = new CreateAlbumRequestValidator();
            var tooMany = Enumerable.Range(0, CreateAlbumRequestValidator.MaxTracksOnCreate + 1).Select(_ => Guid.NewGuid()).ToList();

            Assert.True(validator.Validate(new CreateAlbumRequest("Album", null, null, Guid.NewGuid(), AlbumType.EP, [Guid.NewGuid()])).IsValid);
            Assert.False(validator.Validate(new CreateAlbumRequest("Album", null, null, Guid.NewGuid(), (AlbumType)42)).IsValid);
            Assert.False(validator.Validate(new CreateAlbumRequest("Album", null, null, Guid.NewGuid(), TrackIds: tooMany)).IsValid);
            Assert.False(validator.Validate(new CreateAlbumRequest("Album", null, null, Guid.NewGuid(), TrackIds: [Guid.Empty])).IsValid);
        }

        [Fact]
        public void UpdateAlbum_TypeIsOptionalButMustBeKnown()
        {
            var validator = new UpdateAlbumRequestValidator();

            Assert.True(validator.Validate(new UpdateAlbumRequest("Album", null, null)).IsValid);
            Assert.True(validator.Validate(new UpdateAlbumRequest("Album", null, null, AlbumType.Single)).IsValid);
            Assert.False(validator.Validate(new UpdateAlbumRequest("Album", null, null, (AlbumType)42)).IsValid);
        }
    }
}
