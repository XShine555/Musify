using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class InfrastructureStorageConfiguration
    {
        public const string SectionName = "InfrastructureStorage";

        [Required]
        [Url]
        public required string ServiceUrl { get; set; }

        [Required]
        [MinLength(3)]
        public required string AccessKey { get; set; }

        [Required]
        [MinLength(8)]
        public required string SecretAccessKey { get; set; }

        [Required]
        public bool ForcePathStyle { get; set; }

        [Required]
        public bool UseHttp { get; set; }
    }
}
