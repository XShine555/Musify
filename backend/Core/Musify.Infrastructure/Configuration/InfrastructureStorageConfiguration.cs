using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration;

public class InfrastructureStorageConfiguration
{
    public const string SectionName = "InfrastructureStorage";

    [Url]
    [Required]
    public required string Address { get; set; }

    [Required]
    public required string AccessKey { get; set; }

    [Required]
    public required string SecretAccessKey { get; set; }

    [Required]
    public bool ForcePathStyle { get; set; }

    [Required]
    public bool UseHttp { get; set; }
}
