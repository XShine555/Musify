using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
#pragma warning disable CS8618
    public class DatabaseConfiguration
    {
        public const string SectionName = "Database";

        [Required]
        public string ConnectionString { get; set; }
    }
}