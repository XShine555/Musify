namespace Musify.Application.Configuration
{
    public class StorageConfiguration
    {
        public string BucketName { get; set; } = "musify-S3";

        public StorageRoutes Routes { get; set; } = new StorageRoutes();
    }

    public class StorageRoutes
    {
        public string Uploads { get; set; } = "Uploads";
    }
}