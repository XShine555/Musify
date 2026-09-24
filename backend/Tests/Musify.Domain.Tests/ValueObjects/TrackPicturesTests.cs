using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Domain.Tests.ValueObjects;

public sealed class TrackPicturesTests
{
    [Theory]
    [InlineData(ProcessingStatus.Pending, false)]
    [InlineData(ProcessingStatus.Processing, false)]
    [InlineData(ProcessingStatus.Completed, true)]
    [InlineData(ProcessingStatus.Failed, false)]
    public void IsProcessed_ReflectsProcessingStatus(ProcessingStatus status, bool expected)
    {
        var pictures = new TrackPictures { ProcessingStatus = status };

        Assert.Equal(expected, pictures.IsProcessed);
    }
}
