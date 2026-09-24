using System.ComponentModel.DataAnnotations;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.Configuration
{
    public class WorkerConfiguration : IConfigurationOptions
    {
        public static string SectionName => "Workers";

        [Required]
        public WorkerRoutes Routes { get; set; } = new WorkerRoutes();
    }

    public class WorkerRoutes
    {
        [Required]
        public string TemporaryFilesDirectory { get; set; } = Path.GetTempPath();
    }
}
