using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Services
{
    public sealed class StreamTicketService : IStreamTicketService, IDisposable
    {
        private readonly StreamTicketConfiguration configuration;
        private readonly RSA rsa;
        private readonly SigningCredentials signingCredentials;
        private readonly JsonWebTokenHandler tokenHandler = new();

        public StreamTicketService(StreamTicketConfiguration configuration)
        {
            this.configuration = configuration;

            rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(configuration.PrivateKeyPath));

            var key = new RsaSecurityKey(rsa);
            signingCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        }

        public StreamTicket IssueTicket(long? userId, string keyPrefix, long? maxBytes = null)
        {
            var now = DateTimeOffset.UtcNow;

            var claims = new Dictionary<string, object> { ["prefix"] = keyPrefix };
            if (userId != null)
                claims["sub"] = userId.Value.ToString();
            if (maxBytes != null)
                claims["maxBytes"] = maxBytes.Value.ToString();

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = configuration.Issuer,
                Audience = configuration.Audience,
                IssuedAt = now.UtcDateTime,
                NotBefore = now.UtcDateTime,
                Expires = now.AddSeconds(configuration.TicketTtlSeconds).UtcDateTime,
                Claims = claims,
                SigningCredentials = signingCredentials,
            };

            var token = tokenHandler.CreateToken(descriptor);
            return new StreamTicket(token, configuration.TicketTtlSeconds);
        }

        public void Dispose() => rsa.Dispose();
    }
}
