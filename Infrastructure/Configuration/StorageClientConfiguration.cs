using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class StorageClientConfiguration
    {
        public const string SectionName = "Storage";

        [Required]
        public required string ServiceUrl { get; set; }

        [Required]
        public required string AccessKey { get; set; }

        [Required]
        public required string SecretAccessKey { get; set; }

        [Required]
        public bool ForcePathStyle { get; set; }

        [Required]
        public bool UseHttp { get; set; }
    }
}
