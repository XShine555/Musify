using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class ApplicationStorageConfiguration
    {
        public const string SectionName = "ApplicationStorage";

        [Required]
        [MinLength(3)]
        [MaxLength(63)]
        public required string Bucket { get; set; }
    }
}
