using System.Net;
using System.Net.Http.Json;
using Musify.Api.Tests.TestSupport;
using Musify.Application.Configuration.Responses;
using Xunit;

namespace Musify.Api.Tests.Endpoints
{
    [Collection(ApiCollection.Name)]
    public sealed class ConfigEndpointsTests(ApiTestFixture fixture)
    {
        [Fact]
        public async Task GetPlaybackConfig_WithoutAuthentication_ReturnsConfiguredValues()
        {
            var client = fixture.CreateAnonymousClient();

            var response = await client.GetAsync("/config/playback", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<PlaybackPublicConfigResponse>(TestContext.Current.CancellationToken);
            Assert.NotNull(body);
            Assert.False(body.AllowAnonymousListening);
        }
    }
}
