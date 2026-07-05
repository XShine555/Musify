using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Musify.Api.Authentication;

namespace Musify.Api.OpenApi;

public sealed class OpenApiOptionsSetup(IOptions<AuthenticationConfiguration> options)
    : IConfigureNamedOptions<OpenApiOptions>
{
    private const string SecuritySchemeId = "OAuth2";

    private readonly AuthenticationConfiguration _configuration = options.Value;

    public void Configure(string? name, OpenApiOptions options) => Configure(options);

    public void Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[SecuritySchemeId] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(_configuration.AuthorizationEndpoint),
                        TokenUrl = new Uri(_configuration.TokenEndpoint),
                        Scopes = _configuration.Scopes.ToDictionary(scope => scope, scope => scope),
                    },
                },
            };

            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, _) =>
        {
            var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<IAuthorizeData>()
                .Any();

            var allowsAnonymous = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>()
                .Any();

            if (!requiresAuthorization || allowsAnonymous)
            {
                return Task.CompletedTask;
            }

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SecuritySchemeId, context.Document)] =
                    _configuration.Scopes.ToList(),
            });

            return Task.CompletedTask;
        });
    }
}
