using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Musify.Api.Authentication
{
    public sealed class JwtBearerOptionsSetup(IOptions<AuthenticationConfiguration> options)
        : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly AuthenticationConfiguration configuration = options.Value;

        public void Configure(string? name, JwtBearerOptions options)
        {
            if (name != JwtBearerDefaults.AuthenticationScheme)
                return;

            Configure(options);
        }

        public void Configure(JwtBearerOptions options)
        {
            options.MetadataAddress = configuration.MetadataAddress;
            options.RequireHttpsMetadata = configuration.RequireHttpsMetadata;
            options.Audience = configuration.AudienceAddress;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = configuration.IssuerAddress,
                ValidAudience = configuration.AudienceAddress,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name,
            };
            options.EventsType = typeof(JwtBearerEventsHandler);
        }
    }
}
