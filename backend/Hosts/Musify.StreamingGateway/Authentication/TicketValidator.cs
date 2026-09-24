using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Musify.StreamingGateway.Configuration;

namespace Musify.StreamingGateway.Authentication
{
    public sealed record TicketValidationResult(string Prefix, long? MaxBytes)
    {
        public static readonly TicketValidationResult Invalid = new(string.Empty, null);

        public bool IsValid => Prefix.Length > 0;
    }

    public sealed class TicketValidator : IDisposable
    {
        private readonly RSA rsa;
        private readonly TokenValidationParameters validationParameters;
        private readonly JsonWebTokenHandler handler = new();

        public TicketValidator(StreamTicketValidationConfiguration configuration)
        {
            rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(configuration.PublicKeyPath));

            validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration.Issuer,
                ValidateAudience = true,
                ValidAudience = configuration.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        }

        public async Task<TicketValidationResult> ValidateTicketAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return TicketValidationResult.Invalid;

            var result = await handler.ValidateTokenAsync(token, validationParameters);
            if (!result.IsValid)
                return TicketValidationResult.Invalid;

            var prefix = result.ClaimsIdentity?.FindFirst("prefix")?.Value;
            if (string.IsNullOrEmpty(prefix))
                return TicketValidationResult.Invalid;

            var maxBytesClaim = result.ClaimsIdentity?.FindFirst("maxBytes")?.Value;
            var maxBytes = long.TryParse(maxBytesClaim, out var parsed) ? parsed : (long?)null;

            return new TicketValidationResult(prefix, maxBytes);
        }

        public void Dispose() => rsa.Dispose();
    }
}
