using Musify.Application.Configuration;

namespace Musify.Application.Tests.TestSupport
{
    public static class TestConfigurations
    {
        public static ApplicationStorageConfiguration Storage(string bucket = "test-bucket") =>
            new() { Bucket = bucket };

        public static TrackConfiguration Track() => new();

        public static PlayListConfiguration PlayList() => new();

        public static MixConfiguration Mix() => new();

        public static UploadIntentConfiguration UploadIntent() => new();

        public static StreamGatewayConfiguration StreamGateway(string publicBaseUrl = "https://stream.musify.test") =>
            new() { PublicBaseUrl = publicBaseUrl };

        public static PlaybackConfiguration Playback(bool allowAnonymousListening = false, int anonymousFragmentSeconds = 0) =>
            new() { AllowAnonymousListening = allowAnonymousListening, AnonymousFragmentSeconds = anonymousFragmentSeconds };
    }
}
