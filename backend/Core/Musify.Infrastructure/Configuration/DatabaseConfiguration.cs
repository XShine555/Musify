using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration;

public class DatabaseConfiguration
{
    public const string SectionName = "Database";

    [Required]
    public required string ConnectionString { get; set; }
}
