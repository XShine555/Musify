using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Domain.Tests.ValueObjects;

public sealed class TrackAudioTests
{
    [Theory]
    [InlineData(ProcessingStatus.Pending, false, false, true)]
    [InlineData(ProcessingStatus.Processing, false, false, true)]
    [InlineData(ProcessingStatus.Completed, true, false, false)]
    [InlineData(ProcessingStatus.Failed, false, true, false)]
    public void ComputedFlags_ReflectTranscodeStatus(
        ProcessingStatus status, bool expectedProcessed, bool expectedFailed, bool expectedInProgress)
    {
        var audio = new TrackAudio { TranscodeStatus = status };

        Assert.Equal(expectedProcessed, audio.IsProcessed);
        Assert.Equal(expectedFailed, audio.IsFailed);
        Assert.Equal(expectedInProgress, audio.IsInProgress);
    }
}
