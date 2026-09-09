using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Musify.Api.Tests.TestSupport
{
    /// <summary>
    /// Replaces the real JwtBearer handler in the test host: a request carrying the
    /// <see cref="UserIdHeader"/> header authenticates as that user id, with no real token,
    /// issuer or JWKS endpoint involved. A request without the header stays anonymous, so
    /// endpoints under <c>.RequireAuthorization()</c> correctly answer 401.
    /// </summary>
    public sealed class FakeAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";
        public const string UserIdHeader = "X-Test-User-Id";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserIdHeader, out var userId) || string.IsNullOrEmpty(userId))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId!) };
            if (Request.Headers.TryGetValue("X-Test-User-Name", out var userName) && !string.IsNullOrEmpty(userName))
                claims.Add(new Claim(ClaimTypes.Name, userName!));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
