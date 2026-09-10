using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    /// <summary>Controls whether listeners without a session can stream music at all, and if so,
    /// how much of a track they get to hear before being cut off (a Spotify-style preview).</summary>
    public sealed class PlaybackConfiguration
    {
        public const string SectionName = "Playback";

        /// <summary>When false (the default), every stream endpoint requires a signed-in user, exactly
        /// like before this setting existed. When true, an anonymous request is issued a stream ticket
        /// too, capped by <see cref="AnonymousFragmentSeconds"/> if that is greater than zero.</summary>
        public bool AllowAnonymousListening { get; set; }

        /// <summary>How many seconds of a track an anonymous listener may hear, enforced by the
        /// Streaming Gateway clamping the byte range it will serve for that ticket. Zero (the default)
        /// means no cap: anonymous listeners hear the full track, same as a signed-in user. Ignored
        /// when <see cref="AllowAnonymousListening"/> is false.</summary>
        [Range(0, 3600)]
        public int AnonymousFragmentSeconds { get; set; }

        /// <summary>Bytes per second of the transcoded audio, used to turn <see cref="AnonymousFragmentSeconds"/>
        /// into a byte cap for the stream ticket. Defaults to the app's own transcoder output (128 kbps
        /// AAC, <c>AudioTranscoder:Ffmpeg:AudioBitrate</c>) — update this alongside that setting if it
        /// ever changes, since the two aren't wired together.</summary>
        [Range(1, int.MaxValue)]
        public int EstimatedAudioBytesPerSecond { get; set; } = 16_000;
    }
}
