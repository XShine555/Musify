using Musify.Application.Configuration;
using Musify.Application.Tests.TestSupport;
using Xunit;

namespace Musify.Application.Tests.Configuration
{
    public sealed class GetPlaybackPublicConfigTests
    {
        [Fact]
        public async Task Handle_ReflectsPlaybackConfiguration()
        {
            var playback = TestConfigurations.Playback(allowAnonymousListening: true, anonymousFragmentSeconds: 30);
            var handler = new GetPlaybackPublicConfigQueryHandler(playback);

            var response = await handler.Handle(new GetPlaybackPublicConfigQuery(), TestContext.Current.CancellationToken);

            Assert.True(response.AllowAnonymousListening);
            Assert.Equal(30, response.AnonymousFragmentSeconds);
        }

        [Fact]
        public async Task Handle_DefaultsToDisabled()
        {
            var handler = new GetPlaybackPublicConfigQueryHandler(TestConfigurations.Playback());

            var response = await handler.Handle(new GetPlaybackPublicConfigQuery(), TestContext.Current.CancellationToken);

            Assert.False(response.AllowAnonymousListening);
        }
    }
}
