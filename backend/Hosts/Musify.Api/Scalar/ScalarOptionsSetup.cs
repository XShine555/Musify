using Microsoft.Extensions.Options;
using Musify.Api.Authentication;
using Musify.Api.OpenApi;
using Scalar.AspNetCore;

namespace Musify.Api.Scalar;

public sealed class ScalarOptionsSetup(IOptions<AuthenticationConfiguration> options)
    : IConfigureOptions<ScalarOptions>
{
    private readonly AuthenticationConfiguration configuration = options.Value;

    public void Configure(ScalarOptions scalarOptions)
    {
        scalarOptions
            .WithTitle("Musify API")
            .AddPreferredSecuritySchemes(OpenApiOptionsSetup.SecuritySchemeId)
            .AddAuthorizationCodeFlow(OpenApiOptionsSetup.SecuritySchemeId, flow =>
            {
                flow.ClientId = configuration.ClientId;
                flow.ClientSecret = configuration.ClientSecret;
                flow.AuthorizationUrl = configuration.AuthorizationEndpoint;
                flow.TokenUrl = configuration.TokenEndpoint;
                flow.Pkce = Pkce.Sha256;
                flow.SelectedScopes = configuration.Scopes;
                flow.RedirectUri = configuration.ScalarRedirectUri;
                flow.RefreshUrl = configuration.TokenEndpoint;
            })
            .HideModels();
    }
}
