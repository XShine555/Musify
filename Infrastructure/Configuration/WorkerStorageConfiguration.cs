using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class WorkerStorageConfiguration
    {
        public const string SectionName = "WorkerStorage";

        public Routes Routes { get; set; } = new Routes();

        [Required]
        [MinLength(3)]
        public required string BucketName { get; set; }
    }

    public class Routes
    {
        [Required]
        public string TemporaryFiles { get; set; } = "TemporaryFiles";
    }
}
