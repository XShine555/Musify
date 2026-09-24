using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration;


public sealed class PlaybackConfiguration : IConfigurationOptions
{
    public static string SectionName => "Playback";

    public bool AllowAnonymousListening { get; set; }

    [Range(0, 3600)]
    public int AnonymousFragmentSeconds { get; set; }

    [Range(1, int.MaxValue)]
    public int EstimatedAudioBytesPerSecond { get; set; } = 16_000;
}
