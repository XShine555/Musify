using System.ComponentModel.DataAnnotations;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.Configuration;

public class InfrastructureStorageConfiguration : IConfigurationOptions
{
    public static string SectionName => "InfrastructureStorage";

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
