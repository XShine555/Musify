using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.Validators.Albums;
using Musify.Api.Validators.Tracks;
using Musify.Application.Configuration;
using Xunit;

namespace Musify.Api.Tests.Validators;

public sealed class UploadRequestValidatorTests
{
    private readonly UploadIntentConfiguration configuration = new() { MaxUploadBytes = 1_000 };

    [Fact]
    public void PictureUpload_ValidRequest_Passes()
    {
        var validator = new RequestAlbumPictureUploadRequestValidator(configuration);

        var result = validator.Validate(new RequestAlbumPictureUploadRequest("webp", "image/webp", 500));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("exe", "image/webp", 500L)]
    [InlineData("averyveryveryloongextension", "image/webp", 500L)]
    [InlineData("we bp", "image/webp", 500L)]
    [InlineData("webp", "text/html", 500L)]
    [InlineData("webp", "image/webp", 0L)]
    [InlineData("webp", "image/webp", 1_001L)]
    public void PictureUpload_InvalidRequest_Fails(string fileType, string contentType, long size)
    {
        var validator = new RequestAlbumPictureUploadRequestValidator(configuration);

        var result = validator.Validate(new RequestAlbumPictureUploadRequest(fileType, contentType, size));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TrackUpload_AudioTypesAreCheckedAgainstTheAudioAllowList()
    {
        var validator = new RequestTrackUploadUrlsRequestValidator(configuration);

        Assert.True(validator.Validate(new RequestTrackUploadUrlsRequest("png", "image/png", "mp3", "audio/mpeg", 10, 900)).IsValid);
        Assert.False(validator.Validate(new RequestTrackUploadUrlsRequest("png", "image/png", "png", "image/png")).IsValid);
        Assert.False(validator.Validate(new RequestTrackUploadUrlsRequest("mp3", "audio/mpeg", "mp3", "audio/mpeg")).IsValid);
    }
}
