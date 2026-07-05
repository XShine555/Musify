using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Musify.StreamingGateway.Configuration;

namespace Musify.StreamingGateway.Authentication;

public sealed class TicketValidator : IDisposable
{
    private readonly StreamTicketValidationOptions _options;
    private readonly RSA _rsa;
    private readonly TokenValidationParameters _validationParameters;
    private readonly JsonWebTokenHandler _handler = new();

    public TicketValidator(IOptions<StreamTicketValidationOptions> options)
    {
        _options = options.Value;

        _rsa = RSA.Create();
        _rsa.ImportFromPem(File.ReadAllText(_options.PublicKeyPath));

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(_rsa),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    /// <summary>
    /// Validates the stream ticket and returns the authorized object-key prefix,
    /// or <see langword="null"/> if the ticket is missing, invalid, or carries no prefix.
    /// </summary>
    public async Task<string?> ValidateTicketAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var result = await _handler.ValidateTokenAsync(token, _validationParameters);
        if (!result.IsValid)
            return null;

        return result.Claims.TryGetValue("prefix", out var claim)
               && claim is string prefix
               && !string.IsNullOrEmpty(prefix)
            ? prefix
            : null;
    }

    public void Dispose() => _rsa.Dispose();
}
