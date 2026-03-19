using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class StorageSettings
    {
        public const string SectionName = "Storage";

        [Required]
        public required string BucketName { get; set; }
    }
}
