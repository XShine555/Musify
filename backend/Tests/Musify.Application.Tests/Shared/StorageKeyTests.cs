using Musify.Application.Shared;
using Xunit;

namespace Musify.Application.Tests.Shared
{
    public sealed class StorageKeyTests
    {
        [Fact]
        public void Combine_MultipleSegments_JoinsThemWithForwardSlashes()
        {
            var key = StorageKey.Combine("Tracks", "SmallPictures", "cover.webp");

            Assert.Equal("Tracks/SmallPictures/cover.webp", key);
        }

        [Fact]
        public void Combine_SegmentsWithSurroundingSlashesAndWhitespace_TrimsThem()
        {
            var key = StorageKey.Combine(" Tracks/ ", "/SmallPictures/", "cover.webp");

            Assert.Equal("Tracks/SmallPictures/cover.webp", key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Combine_BlankSegment_IsSkipped(string? blank)
        {
            var key = StorageKey.Combine("Tracks", blank!, "cover.webp");

            Assert.Equal("Tracks/cover.webp", key);
        }

        [Fact]
        public void Combine_NoSegments_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, StorageKey.Combine());
        }
    }
}
