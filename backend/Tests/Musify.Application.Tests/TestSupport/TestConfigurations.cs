using Musify.Application.Configuration;

namespace Musify.Application.Tests.TestSupport;

/// <summary>
/// Configuration POCOs with every <c>required</c> member filled and the rest left at their
/// production defaults, so handler tests don't have to know which fields matter for them.
/// </summary>
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
}
