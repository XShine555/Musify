using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class StorageSettings
    {
        public const string SectionName = "Storage";

        [Required]
        [MinLength(3)]
        [MaxLength(63)]
        public required string BucketName { get; set; }
    }
}
