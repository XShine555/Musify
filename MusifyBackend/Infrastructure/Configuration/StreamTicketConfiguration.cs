using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class StreamTicketConfiguration
    {
        public const string SectionName = "StreamTicket";

        [Required]
        public required string PrivateKeyPath { get; set; }

        [Required]
        public string Audience { get; set; } = "media-gateway";

        [Required]
        public string Issuer { get; set; } = "musify-webapi";

        [Range(60, 86400)]
        public int TicketTtlSeconds { get; set; } = 3600;
    }
}
