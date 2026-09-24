using System.ComponentModel.DataAnnotations;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.Configuration
{
    public class DatabaseConfiguration : IConfigurationOptions
    {
        public static string SectionName => "Database";

        [Required]
        public required string ConnectionString { get; set; }
    }
}
