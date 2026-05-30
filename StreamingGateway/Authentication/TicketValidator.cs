using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Musify.StreamingGateway.Configuration;

namespace Musify.StreamingGateway.Authentication;

public sealed class TicketValidator : IDisposable
{
    private readonly StreamTicketValidationOptions options;
    private readonly RSA rsa;
    private readonly TokenValidationParameters validationParameters;
    private readonly JsonWebTokenHandler handler = new();

    public TicketValidator(IOptions<StreamTicketValidationOptions> options)
    {
        this.options = options.Value;

        rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(this.options.PublicKeyPath));

        validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = this.options.Issuer,
            ValidateAudience = true,
            ValidAudience = this.options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    public async Task<string?> TryGetPrefixAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var result = await handler.ValidateTokenAsync(token, validationParameters);
        if (!result.IsValid)
            return null;

        return result.Claims.TryGetValue("prefix", out var prefix)
            ? prefix as string
            : null;
    }

    public void Dispose() => rsa.Dispose();
}
