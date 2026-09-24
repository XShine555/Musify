using Microsoft.Extensions.Options;
using Musify.Api.Authentication;
using Scalar.AspNetCore;

namespace Musify.Api.Scalar;

public sealed class ScalarOptionsSetup(IOptions<AuthenticationConfiguration> options)
    : IConfigureOptions<ScalarOptions>
{
    private const string SecuritySchemeId = "OAuth2";

    private readonly AuthenticationConfiguration _configuration = options.Value;

    public void Configure(ScalarOptions scalarOptions)
    {
        scalarOptions
            .WithTitle("Musify API")
            .AddPreferredSecuritySchemes(SecuritySchemeId)
            .AddAuthorizationCodeFlow(SecuritySchemeId, flow =>
            {
                flow.ClientId = _configuration.ClientId;
                flow.ClientSecret = _configuration.ClientSecret;
                flow.AuthorizationUrl = _configuration.AuthorizationEndpoint;
                flow.TokenUrl = _configuration.TokenEndpoint;
                flow.Pkce = Pkce.Sha256;
                flow.SelectedScopes = _configuration.Scopes;
                flow.RedirectUri = _configuration.ScalarRedirectUri;
                flow.RefreshUrl = _configuration.TokenEndpoint;
            })
            .HideModels();
    }
}
