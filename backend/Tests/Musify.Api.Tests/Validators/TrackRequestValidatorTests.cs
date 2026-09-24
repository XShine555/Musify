using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.Validators.Tracks;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Api.Tests.Validators;

public sealed class TrackRequestValidatorTests
{
    [Fact]
    public void CreateTrack_ValidRequest_Passes()
    {
        var request = new CreateTrackRequest("Song", Guid.NewGuid(), Guid.NewGuid(), [Genre.Pop, Genre.Rock]);

        Assert.True(new CreateTrackRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void CreateTrack_NoTags_Fails()
    {
        var request = new CreateTrackRequest("Song", Guid.NewGuid(), Guid.NewGuid(), []);

        Assert.False(new CreateTrackRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void CreateTrack_IncompatibleTags_Fails()
    {
        var request = new CreateTrackRequest("Song", Guid.NewGuid(), Guid.NewGuid(), [Genre.Classical, Genre.Metal]);

        Assert.False(new CreateTrackRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RecordListeningProgress_InvalidSeconds_Fails(double seconds)
    {
        Assert.False(new RecordListeningProgressRequestValidator().Validate(new RecordListeningProgressRequest(seconds)).IsValid);
    }

    [Fact]
    public void RecordListeningProgress_ZeroSeconds_Passes()
    {
        Assert.True(new RecordListeningProgressRequestValidator().Validate(new RecordListeningProgressRequest(0)).IsValid);
    }
}
