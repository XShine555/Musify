using System.Globalization;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Musify.StreamingGateway.Authentication;
using Musify.StreamingGateway.Configuration;
using Xunit;

namespace Musify.StreamingGateway.Tests.Authentication;

public sealed class TicketValidatorTests : IDisposable
{
    private const string Issuer = "musify-webapi";
    private const string Audience = "media-gateway";

    private readonly string publicKeyPath = Path.GetTempFileName();
    private readonly RSA rsa = RSA.Create(2048);
    private readonly JsonWebTokenHandler tokenHandler = new();

    public TicketValidatorTests() => File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());

    public void Dispose()
    {
        rsa.Dispose();
        File.Delete(publicKeyPath);
    }

    private TicketValidator CreateValidator() => new(new StreamTicketValidationConfiguration
    {
        PublicKeyPath = publicKeyPath,
        Audience = Audience,
        Issuer = Issuer
    });

    private string IssueToken(string prefix = "Tracks/ProcessedAudios/abc/", string issuer = Issuer, string audience = Audience,
        DateTime? expires = null, DateTime? notBefore = null, long? maxBytes = null)
    {
        var claims = new Dictionary<string, object> { ["prefix"] = prefix };
        if (maxBytes != null)
            claims["maxBytes"] = maxBytes.Value.ToString(CultureInfo.InvariantCulture);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            NotBefore = notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            Claims = claims,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256),
        };
        return tokenHandler.CreateToken(descriptor);
    }

    [Fact]
    public async Task ValidateTicketAsync_ValidToken_ReturnsThePrefixClaim()
    {
        var token = IssueToken(prefix: "Tracks/ProcessedAudios/xyz/");

        var result = await CreateValidator().ValidateTicketAsync(token);

        Assert.True(result.IsValid);
        Assert.Equal("Tracks/ProcessedAudios/xyz/", result.Prefix);
        Assert.Null(result.MaxBytes);
    }

    [Fact]
    public async Task ValidateTicketAsync_TokenWithMaxBytesClaim_ReturnsIt()
    {
        var token = IssueToken(maxBytes: 480_000);

        var result = await CreateValidator().ValidateTicketAsync(token);

        Assert.True(result.IsValid);
        Assert.Equal(480_000, result.MaxBytes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateTicketAsync_BlankToken_ReturnsInvalid(string? token)
    {
        var result = await CreateValidator().ValidateTicketAsync(token);

        Assert.False(result.IsValid);
        Assert.Equal(string.Empty, result.Prefix);
    }

    [Fact]
    public async Task ValidateTicketAsync_MalformedToken_ReturnsInvalid()
    {
        var result = await CreateValidator().ValidateTicketAsync("not-a-real-jwt");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateTicketAsync_ExpiredToken_ReturnsInvalid()
    {
        var token = IssueToken(expires: DateTime.UtcNow.AddMinutes(-40), notBefore: DateTime.UtcNow.AddMinutes(-50));

        var result = await CreateValidator().ValidateTicketAsync(token);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateTicketAsync_WrongIssuer_ReturnsInvalid()
    {
        var token = IssueToken(issuer: "someone-else");

        var result = await CreateValidator().ValidateTicketAsync(token);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateTicketAsync_WrongAudience_ReturnsInvalid()
    {
        var token = IssueToken(audience: "someone-else");

        var result = await CreateValidator().ValidateTicketAsync(token);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateTicketAsync_SignedWithADifferentKey_ReturnsInvalid()
    {
        using var otherKey = RSA.Create(2048);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { ["prefix"] = "Tracks/ProcessedAudios/abc/" },
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(otherKey), SecurityAlgorithms.RsaSha256),
        };
        var token = tokenHandler.CreateToken(descriptor);

        var result = await CreateValidator().ValidateTicketAsync(token);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateTicketAsync_MissingPrefixClaim_ReturnsInvalid()
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256),
        };
        var token = tokenHandler.CreateToken(descriptor);

        var result = await CreateValidator().ValidateTicketAsync(token);

        Assert.False(result.IsValid);
    }
}
