using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
#pragma warning disable CS8618
    public class StorageConfiguration
    {
        public const string SectionName = "Storage";

        [Required]
        public string BucketName { get; set; }

        [Required]
        public string ServiceUrl { get; set; }

        [Required]
        public string AccessKey { get; set; }

        [Required]
        public string SecretAccessKey { get; set; }

        [Required]
        public bool ForcePathStyle { get; set; }

        [Required]
        public bool UseHttp { get; set; }
    }
}