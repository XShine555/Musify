using System.ComponentModel.DataAnnotations;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.Configuration
{
    public class MassTransitConfiguration : IConfigurationOptions
    {
        public static string SectionName => "MassTransit";

        [Required]
        public required string Address { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
