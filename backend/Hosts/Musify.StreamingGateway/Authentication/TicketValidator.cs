using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Musify.StreamingGateway.Configuration;

namespace Musify.StreamingGateway.Authentication;

public sealed record TicketValidationResult(string Prefix, long? MaxBytes)
{
    public static readonly TicketValidationResult Invalid = new(string.Empty, null);

    public bool IsValid => Prefix.Length > 0;
}

public sealed class TicketValidator : IDisposable
{
    private readonly StreamTicketValidationConfiguration _options;
    private readonly RSA _rsa;
    private readonly TokenValidationParameters _validationParameters;
    private readonly JsonWebTokenHandler _handler = new();

    public TicketValidator(StreamTicketValidationConfiguration options)
    {
        _options = options;

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

    public async Task<TicketValidationResult> ValidateTicketAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return TicketValidationResult.Invalid;

        var result = await _handler.ValidateTokenAsync(token, _validationParameters);
        if (!result.IsValid)
            return TicketValidationResult.Invalid;

        var prefix = result.ClaimsIdentity?.FindFirst("prefix")?.Value;
        if (string.IsNullOrEmpty(prefix))
            return TicketValidationResult.Invalid;

        var maxBytesClaim = result.ClaimsIdentity?.FindFirst("maxBytes")?.Value;
        var maxBytes = long.TryParse(maxBytesClaim, out var parsed) ? parsed : (long?)null;

        return new TicketValidationResult(prefix, maxBytes);
    }

    public void Dispose() => _rsa.Dispose();
}
