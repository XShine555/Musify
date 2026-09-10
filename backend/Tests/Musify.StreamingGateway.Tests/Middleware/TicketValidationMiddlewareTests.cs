using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Musify.StreamingGateway.Authentication;
using Musify.StreamingGateway.Configuration;
using Musify.StreamingGateway.Middleware;
using Xunit;

namespace Musify.StreamingGateway.Tests.Middleware
{
    public sealed class TicketValidationMiddlewareTests : IDisposable
    {
        private const string Prefix = "Tracks/ProcessedAudios/abc";

        private readonly string publicKeyPath = Path.GetTempFileName();
        private readonly RSA rsa = RSA.Create(2048);
        private readonly StreamTicketValidationOptions options;

        public TicketValidationMiddlewareTests()
        {
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
            options = new StreamTicketValidationOptions
            {
                PublicKeyPath = publicKeyPath,
                Audience = "media-gateway",
                Issuer = "musify-webapi",
            };
        }

        public void Dispose()
        {
            rsa.Dispose();
            File.Delete(publicKeyPath);
        }

        private string IssueValidToken(string prefix = Prefix, long? maxBytes = null)
        {
            var claims = new Dictionary<string, object> { ["prefix"] = prefix };
            if (maxBytes is not null)
                claims["maxBytes"] = maxBytes.Value.ToString();

            return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = options.Issuer,
                Audience = options.Audience,
                NotBefore = DateTime.UtcNow.AddMinutes(-1),
                Expires = DateTime.UtcNow.AddMinutes(5),
                Claims = claims,
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256),
            });
        }

        private (TicketValidationMiddleware Middleware, Func<bool> WasNextCalled) CreateMiddleware()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            var validator = new TicketValidator(Options.Create(options));
            var middleware = new TicketValidationMiddleware(next, validator, Options.Create(options), NullLogger<TicketValidationMiddleware>.Instance);
            return (middleware, () => nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_PathOutsideMediaPrefix_PassesThroughWithoutValidatingATicket()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = "/health";

            await middleware.InvokeAsync(context);

            Assert.True(wasNextCalled());
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_NoTicket_ReturnsUnauthorized()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = $"/media/{Prefix}/audio.m4a";

            await middleware.InvokeAsync(context);

            Assert.False(wasNextCalled());
            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ValidTicketAsQueryParameter_ForTheRequestedObject_PassesThroughAndStripsTheTicket()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = $"/media/{Prefix}/audio.m4a";
            context.Request.QueryString = new QueryString($"?t={IssueValidToken()}&keep=1");

            await middleware.InvokeAsync(context);

            Assert.True(wasNextCalled());
            Assert.Equal("keep=1", context.Request.QueryString.Value?.TrimStart('?'));
        }

        [Fact]
        public async Task InvokeAsync_ValidTicketAsHeader_PassesThrough()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = $"/media/{Prefix}/audio.m4a";
            context.Request.Headers["X-Stream-Ticket"] = IssueValidToken();

            await middleware.InvokeAsync(context);

            Assert.True(wasNextCalled());
        }

        [Fact]
        public async Task InvokeAsync_ObjectKeyOutsideAuthorizedPrefix_ReturnsForbidden()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = "/media/Tracks/ProcessedAudios/someone-elses-track/audio.m4a";
            context.Request.QueryString = new QueryString($"?t={IssueValidToken()}");

            await middleware.InvokeAsync(context);

            Assert.False(wasNextCalled());
            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        }

        [Theory]
        [InlineData($"/media/{Prefix}/../../etc/passwd")]
        [InlineData($"/media/{Prefix}/./audio.m4a")]
        public async Task InvokeAsync_PathTraversalSegment_ReturnsBadRequest(string path)
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.QueryString = new QueryString($"?t={IssueValidToken()}");

            await middleware.InvokeAsync(context);

            Assert.False(wasNextCalled());
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_TicketPrefixExactlyMatchesTheRequestedObject_PassesThrough()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = $"/media/{Prefix}";
            context.Request.QueryString = new QueryString($"?t={IssueValidToken()}");

            await middleware.InvokeAsync(context);

            Assert.True(wasNextCalled());
        }

        [Fact]
        public async Task InvokeAsync_TicketWithMaxBytes_NoRangeRequested_SynthesizesABoundedRange()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = $"/media/{Prefix}/audio.m4a";
            context.Request.QueryString = new QueryString($"?t={IssueValidToken(maxBytes: 480_000)}");

            await middleware.InvokeAsync(context);

            Assert.True(wasNextCalled());
            Assert.Equal("bytes=0-479999", context.Request.Headers.Range.ToString());
        }

        [Fact]
        public async Task InvokeAsync_TicketWithMaxBytes_RangeWithinCap_ClampsTheEndOnly()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = $"/media/{Prefix}/audio.m4a";
            context.Request.QueryString = new QueryString($"?t={IssueValidToken(maxBytes: 480_000)}");
            context.Request.Headers.Range = "bytes=1000-";

            await middleware.InvokeAsync(context);

            Assert.True(wasNextCalled());
            Assert.Equal("bytes=1000-479999", context.Request.Headers.Range.ToString());
        }

        [Fact]
        public async Task InvokeAsync_TicketWithMaxBytes_RangeStartsAtOrPastTheCap_ReturnsRangeNotSatisfiable()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = $"/media/{Prefix}/audio.m4a";
            context.Request.QueryString = new QueryString($"?t={IssueValidToken(maxBytes: 480_000)}");
            context.Request.Headers.Range = "bytes=480000-";

            await middleware.InvokeAsync(context);

            Assert.False(wasNextCalled());
            Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_TicketWithoutMaxBytes_LeavesTheRequestedRangeUntouched()
        {
            var (middleware, wasNextCalled) = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Path = $"/media/{Prefix}/audio.m4a";
            context.Request.QueryString = new QueryString($"?t={IssueValidToken()}");
            context.Request.Headers.Range = "bytes=1000-2000";

            await middleware.InvokeAsync(context);

            Assert.True(wasNextCalled());
            Assert.Equal("bytes=1000-2000", context.Request.Headers.Range.ToString());
        }
    }
}
