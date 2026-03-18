using Microsoft.Extensions.Configuration;

namespace Musify.Application.Configuration
{
    public class StorageConfiguration
    {
        public const string SectionName = "Storage";

        public string BucketName { get; set; } = "musify-S3";

        public StorageRoutes Routes { get; set; } = new StorageRoutes();

        public StorageConfiguration Load(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName).Get<StorageConfiguration>()
                ?? throw new InvalidOperationException($"{SectionName} configuration section not found.");
            
            Validate();

            return section;
        }

        void Validate()
        {
            EnsureValue(BucketName, nameof(BucketName));
            EnsureValue(Routes.Uploads, $"{SectionName}:Route:Upload");
        }

        void EnsureValue(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"{name} configuration value not found.");
        }
    }

    public class StorageRoutes
    {
        public string Uploads { get; set; } = "Uploads";
    }
}