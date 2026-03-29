using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class MassTransitConfiguration
    {
        public const string SectionName = "Messaging";

        [Required]
        public required string Host { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
