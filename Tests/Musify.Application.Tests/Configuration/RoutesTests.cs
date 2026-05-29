using FluentAssertions;
using Musify.Application.Configuration;
using Xunit;

namespace Musify.Application.Tests.Configuration;

public sealed class RoutesTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void TrackRoutes_default_picture_and_audio_paths()
    {
        var routes = new TrackRoutes();

        routes.OriginalPicturesPath.Should().Be("Tracks/OriginalPictures");
        routes.OriginalAudiosPath.Should().Be("Tracks/OriginalAudios");
        routes.ProcessedAudiosPath.Should().Be("Tracks/ProcessedAudios");
    }

    [Fact]
    public void TrackRoutes_BuildOriginalPicturePath_with_user_nests_under_uploads()
    {
        var routes = new TrackRoutes();

        routes.BuildOriginalPicturePath(UserId, "cover.webp")
            .Should().Be($"uploads/{UserId}/Tracks/OriginalPictures/cover.webp");
    }

    [Fact]
    public void TrackRoutes_BuildOriginalAudioPath_with_user_nests_under_uploads()
    {
        var routes = new TrackRoutes();

        routes.BuildOriginalAudioPath(UserId, "song.mp3")
            .Should().Be($"uploads/{UserId}/Tracks/OriginalAudios/song.mp3");
    }

    [Fact]
    public void TrackRoutes_BuildTempPicturePath_uses_prefix_user_and_parent_folder()
    {
        var routes = new TrackRoutes();

        routes.BuildTempPicturePath("temp", UserId, "obj.png")
            .Should().Be($"temp/{UserId}/Tracks/obj.png");
    }

    [Fact]
    public void CombineKey_trims_slashes_and_whitespace_per_segment()
    {
        var routes = new TrackRoutes();

        var result = routes.BuildTempPicturePath("  /temp/ ", UserId, "/obj.png\\");

        result.Should().Be($"temp/{UserId}/Tracks/obj.png");
    }

    [Fact]
    public void CombineKey_drops_empty_and_whitespace_segments()
    {
        var routes = new TrackRoutes();

        var result = routes.BuildTempPicturePath("   ", UserId, "obj.png");

        result.Should().Be($"{UserId}/Tracks/obj.png");
    }

    [Fact]
    public void PlayListRoutes_BuildOriginalPicturePath_with_user_nests_under_uploads()
    {
        var routes = new PlayListRoutes();

        routes.BuildOriginalPicturePath(UserId, "cover.webp")
            .Should().Be($"uploads/{UserId}/PlayLists/OriginalPictures/cover.webp");
    }

    [Fact]
    public void PlayListRoutes_size_paths_use_parent_folder()
    {
        var routes = new PlayListRoutes();

        routes.SmallPicturesPath.Should().Be("PlayLists/SmallPictures");
        routes.MediumPicturesPath.Should().Be("PlayLists/MediumPictures");
        routes.LargePicturesPath.Should().Be("PlayLists/LargePictures");
    }
}
