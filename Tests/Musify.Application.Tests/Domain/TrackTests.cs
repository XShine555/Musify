using FluentAssertions;
using Musify.Domain.ValueObjects;
using Musify.Application.Tests.Infrastructure;
using Xunit;

namespace Musify.Application.Tests.Domain;

public sealed class TrackTests
{
    [Fact]
    public void IsPicturesProcessed_is_true_only_when_completed()
    {
        var track = TestData.Track();

        track.PicturesProcessingStatus = ProcessingStatus.Completed;
        track.IsPicturesProcessed.Should().BeTrue();

        track.PicturesProcessingStatus = ProcessingStatus.Processing;
        track.IsPicturesProcessed.Should().BeFalse();

        track.PicturesProcessingStatus = ProcessingStatus.Failed;
        track.IsPicturesProcessed.Should().BeFalse();
    }

    [Fact]
    public void IsAudioProcessed_is_true_only_when_completed()
    {
        var track = TestData.Track();

        track.AudioTranscodeProcessingStatus = ProcessingStatus.Completed;
        track.IsAudioProcessed.Should().BeTrue();

        track.AudioTranscodeProcessingStatus = ProcessingStatus.Pending;
        track.IsAudioProcessed.Should().BeFalse();
    }

    [Fact]
    public void Defaults_are_active_pending_and_zero_retries()
    {
        var track = new Musify.Domain.Entities.Track
        {
            Title = "t",
            NormalizedTitle = "T",
            OriginalPictureName = "p.webp",
            OriginalAudioName = "a.mp3",
        };

        track.LifeCycleStatus.Should().Be(LifeCycleStatus.Active);
        track.PicturesProcessingStatus.Should().Be(ProcessingStatus.Pending);
        track.AudioTranscodeProcessingStatus.Should().Be(ProcessingStatus.Pending);
        track.RetryCount.Should().Be(0);
        track.Id.Should().NotBe(Guid.Empty);
    }
}

public sealed class EnumValueRegressionTests
{
    [Fact]
    public void ProcessingStatus_values_are_stable()
    {
        ((int)ProcessingStatus.Pending).Should().Be(0);
        ((int)ProcessingStatus.Processing).Should().Be(1);
        ((int)ProcessingStatus.Completed).Should().Be(2);
        ((int)ProcessingStatus.Failed).Should().Be(3);
    }

    [Fact]
    public void LifeCycleStatus_values_are_stable()
    {
        ((int)LifeCycleStatus.Active).Should().Be(0);
        ((int)LifeCycleStatus.Removing).Should().Be(1);
    }

    [Fact]
    public void UploadIntentStatus_values_are_stable()
    {
        ((int)UploadIntentStatus.Issued).Should().Be(0);
        ((int)UploadIntentStatus.Consumed).Should().Be(1);
        ((int)UploadIntentStatus.Expired).Should().Be(2);
    }

    [Fact]
    public void UploadIntentPurpose_values_are_stable()
    {
        ((int)UploadIntentPurpose.PlayListPicture).Should().Be(0);
        ((int)UploadIntentPurpose.TrackPicture).Should().Be(1);
        ((int)UploadIntentPurpose.TrackAudio).Should().Be(2);
    }
}
