using System.Security.Claims;
using System.Security.Cryptography;
using Ardalis.Result;
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

    public async Task<Result<string>> ValidateTicketAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Unauthorized();

        var result = await _handler.ValidateTokenAsync(token, _validationParameters);
        if (!result.IsValid)
            return Result.Unauthorized();

        if (!result.Claims.TryGetValue("prefix", out var claim))
            return Result.Unauthorized();

        if (claim is not string prefix)
            return Result.Unauthorized();

        if (string.IsNullOrEmpty(prefix))
            return Result.Unauthorized();

        return Result.Success(prefix);
    }

    public void Dispose() => _rsa.Dispose();
}
