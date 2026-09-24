using Musify.Application.Configuration;
using Xunit;

namespace Musify.Application.Tests.Configuration
{
    public sealed class PicturesConfigurationTests
    {
        [Fact]
        public void AlbumRoutes_BuildTheSameKeysAsBefore()
        {
            var routes = new AlbumConfiguration().Routes;

            Assert.Equal("Albums/SmallPictures/a.webp", routes.BuildPicturePath(PictureSize.Small, "a.webp"));
            Assert.Equal("Albums/MediumPictures/a.webp", routes.BuildPicturePath(PictureSize.Medium, "a.webp"));
            Assert.Equal("Albums/LargePictures/a.webp", routes.BuildPicturePath(PictureSize.Large, "a.webp"));
            Assert.Equal("uploads/7/Albums/OriginalPictures/a.webp", routes.BuildOriginalPicturePath(7, "a.webp"));
            Assert.Equal("temp/7/Albums/obj.webp", routes.BuildTempPath("temp", 7, "obj.webp"));
        }

        [Fact]
        public void TrackRoutes_KeepTheTrackFolderLayout()
        {
            var routes = new TrackConfiguration().Routes;

            Assert.Equal("Tracks/ProcessedAudios/folder", routes.BuildProcessedAudioPath("folder"));
            Assert.Equal("uploads/7/Tracks/OriginalAudios/song.mp3", routes.BuildOriginalAudioPath(7, "song.mp3"));
            Assert.Equal("Tracks/PresetSmallPicture.webp", routes.PresetSmallPicturePath);
        }

        [Fact]
        public void ToImageSizes_UsesTheRoutesFolders()
        {
            var configuration = new PlayListConfiguration();

            var sizes = configuration.PicturesSizes.ToImageSizes(configuration.Routes);

            Assert.Equal("PlayLists/SmallPictures", sizes.Small.SavePath);
            Assert.Equal(128, sizes.Small.Width);
            Assert.Equal(512, sizes.Large.Width);
        }

        [Theory]
        [InlineData("small", PictureSize.Small)]
        [InlineData("LARGE", PictureSize.Large)]
        [InlineData("medium", PictureSize.Medium)]
        [InlineData("whatever", PictureSize.Medium)]
        [InlineData(null, PictureSize.Medium)]
        public void PictureSizeParser_DefaultsToMedium(string? value, PictureSize expected) =>
            Assert.Equal(expected, PictureSizeParser.Parse(value));
    }
}
