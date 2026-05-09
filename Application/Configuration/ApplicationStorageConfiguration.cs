using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class ApplicationStorageConfiguration
    {
        public const string SectionName = "ApplicationStorage";

        [Required]
        [StringLength(128, MinimumLength = 1)]
        public required string Bucket { get; set; }
    }
}
