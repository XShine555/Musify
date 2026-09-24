using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Services;
using Xunit;

namespace Musify.Infrastructure.Tests.Services;

public sealed class StreamTicketServiceTests : IDisposable
{
    private readonly string privateKeyPath = Path.GetTempFileName();
    private readonly RSA rsa = RSA.Create(2048);

    public StreamTicketServiceTests() => File.WriteAllText(privateKeyPath, rsa.ExportPkcs8PrivateKeyPem());

    public void Dispose()
    {
        rsa.Dispose();
        File.Delete(privateKeyPath);
    }

    private StreamTicketService CreateService(int ticketTtlSeconds = 3600) => new(new StreamTicketConfiguration
    {
        PrivateKeyPath = privateKeyPath,
        Audience = "media-gateway",
        Issuer = "musify-webapi",
        TicketTtlSeconds = ticketTtlSeconds
    });

    [Fact]
    public void IssueTicket_ReturnsATokenSignedWithTheConfiguredKeyAndClaims()
    {
        using var service = CreateService(ticketTtlSeconds: 120);

        var ticket = service.IssueTicket(userId: 42, keyPrefix: "Tracks/ProcessedAudios/abc/");

        Assert.Equal(120, ticket.ExpiresInSeconds);

        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(ticket.Token);
        Assert.Equal("42", jwt.GetClaim("sub").Value);
        Assert.Equal("Tracks/ProcessedAudios/abc/", jwt.GetClaim("prefix").Value);
        Assert.Equal("musify-webapi", jwt.Issuer);
        Assert.Contains("media-gateway", jwt.Audiences);
    }

    [Fact]
    public void IssueTicket_AnonymousUser_OmitsTheSubClaim()
    {
        using var service = CreateService();

        var ticket = service.IssueTicket(userId: null, keyPrefix: "Tracks/ProcessedAudios/abc/");

        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(ticket.Token);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "sub");
        Assert.Equal("Tracks/ProcessedAudios/abc/", jwt.GetClaim("prefix").Value);
    }

    [Fact]
    public void IssueTicket_WithMaxBytes_IncludesTheMaxBytesClaim()
    {
        using var service = CreateService();

        var ticket = service.IssueTicket(userId: null, keyPrefix: "Tracks/ProcessedAudios/abc/", maxBytes: 480_000);

        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(ticket.Token);
        Assert.Equal("480000", jwt.GetClaim("maxBytes").Value);
    }

    [Fact]
    public void IssueTicket_WithoutMaxBytes_OmitsTheMaxBytesClaim()
    {
        using var service = CreateService();

        var ticket = service.IssueTicket(userId: 1, keyPrefix: "Tracks/ProcessedAudios/abc/");

        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(ticket.Token);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "maxBytes");
    }

    [Fact]
    public async Task IssueTicket_TokenValidatesAgainstTheMatchingPublicKey()
    {
        using var service = CreateService();
        var ticket = service.IssueTicket(userId: 1, keyPrefix: "Tracks/ProcessedAudios/xyz/");

        var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        using var verifyingRsa = RSA.Create();
        verifyingRsa.ImportFromPem(publicKeyPem);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(ticket.Token, new TokenValidationParameters
        {
            ValidIssuer = "musify-webapi",
            ValidAudience = "media-gateway",
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            IssuerSigningKey = new RsaSecurityKey(verifyingRsa),
        });

        Assert.True(result.IsValid);
    }
}
