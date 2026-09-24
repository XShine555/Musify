using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration;

public class WorkerConfiguration
{
    public const string SectionName = "Workers";

    [Required]
    public WorkerRoutes Routes { get; set; } = new WorkerRoutes();
}

public class WorkerRoutes
{
    [Required]
    public string TemporaryFilesDirectory { get; set; } = Path.GetTempPath();
}
