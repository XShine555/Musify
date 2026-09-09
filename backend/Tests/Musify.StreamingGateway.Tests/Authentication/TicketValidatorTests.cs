using System.Security.Cryptography;
using Microsoft.Extensions.Options;
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

    private TicketValidator CreateValidator() => new(Options.Create(new StreamTicketValidationOptions
    {
        PublicKeyPath = publicKeyPath,
        Audience = Audience,
        Issuer = Issuer
    }));

    private string IssueToken(string prefix = "Tracks/ProcessedAudios/abc/", string issuer = Issuer, string audience = Audience,
        DateTime? expires = null, DateTime? notBefore = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            NotBefore = notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { ["prefix"] = prefix },
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256),
        };
        return tokenHandler.CreateToken(descriptor);
    }

    [Fact]
    public async Task ValidateTicketAsync_ValidToken_ReturnsThePrefixClaim()
    {
        var token = IssueToken(prefix: "Tracks/ProcessedAudios/xyz/");

        var prefix = await CreateValidator().ValidateTicketAsync(token);

        Assert.Equal("Tracks/ProcessedAudios/xyz/", prefix);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateTicketAsync_BlankToken_ReturnsEmptyString(string? token)
    {
        var prefix = await CreateValidator().ValidateTicketAsync(token);

        Assert.Equal(string.Empty, prefix);
    }

    [Fact]
    public async Task ValidateTicketAsync_MalformedToken_ReturnsEmptyString()
    {
        var prefix = await CreateValidator().ValidateTicketAsync("not-a-real-jwt");

        Assert.Equal(string.Empty, prefix);
    }

    [Fact]
    public async Task ValidateTicketAsync_ExpiredToken_ReturnsEmptyString()
    {
        var token = IssueToken(expires: DateTime.UtcNow.AddMinutes(-40), notBefore: DateTime.UtcNow.AddMinutes(-50));

        var prefix = await CreateValidator().ValidateTicketAsync(token);

        Assert.Equal(string.Empty, prefix);
    }

    [Fact]
    public async Task ValidateTicketAsync_WrongIssuer_ReturnsEmptyString()
    {
        var token = IssueToken(issuer: "someone-else");

        var prefix = await CreateValidator().ValidateTicketAsync(token);

        Assert.Equal(string.Empty, prefix);
    }

    [Fact]
    public async Task ValidateTicketAsync_WrongAudience_ReturnsEmptyString()
    {
        var token = IssueToken(audience: "someone-else");

        var prefix = await CreateValidator().ValidateTicketAsync(token);

        Assert.Equal(string.Empty, prefix);
    }

    [Fact]
    public async Task ValidateTicketAsync_SignedWithADifferentKey_ReturnsEmptyString()
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

        var prefix = await CreateValidator().ValidateTicketAsync(token);

        Assert.Equal(string.Empty, prefix);
    }

    [Fact]
    public async Task ValidateTicketAsync_MissingPrefixClaim_ReturnsEmptyString()
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

        var prefix = await CreateValidator().ValidateTicketAsync(token);

        Assert.Equal(string.Empty, prefix);
    }
}
