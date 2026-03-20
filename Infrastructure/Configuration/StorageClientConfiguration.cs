using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class StorageClientConfiguration
    {
        public const string SectionName = "Storage";

        [Required]
        [Url]
        public required string ServiceUrl { get; set; }

        [Required]
        [MinLength(3)]
        public required string AccessKey { get; set; }

        [Required]
        [MinLength(8)]
        public required string SecretAccessKey { get; set; }

        public bool ForcePathStyle { get; set; }

        public bool UseHttp { get; set; }
    }
}
