using Microsoft.Extensions.Configuration;

namespace Musify.Application.Configuration
{
    public class StorageConfiguration
    {
        public const string SectionName = "Storage";

        public string ServiceUrl { get; set; } = string.Empty;

        public string AccessKey { get; set; } = string.Empty;

        public string SecretAccessKey { get; set; } = string.Empty;

        public string BucketName { get; set; } = string.Empty;

        public bool ForcePathStyle { get; set; }

        public bool UseHttp { get; set; }

        public StorageRoutes Routes { get; set; } = new StorageRoutes();

        public static StorageConfiguration Load(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName).Get<StorageConfiguration>()
                ?? throw new InvalidOperationException($"{SectionName} configuration section not found.");
            
            Validate(section);
            return section;
        }

        static void Validate(StorageConfiguration configuration)
        {
            EnsureValue(configuration.BucketName, nameof(BucketName));
            EnsureValue(configuration.Routes.Uploads, $"{SectionName}:Route:Upload");

            EnsureValue(configuration.ServiceUrl, nameof(ServiceUrl));
            EnsureValue(configuration.AccessKey, nameof(AccessKey));
            EnsureValue(configuration.SecretAccessKey, nameof(SecretAccessKey));
        }

        static void EnsureValue(string value, string name)
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