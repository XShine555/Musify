using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Storage
{
    public static class StorageDependencyInjection
    {
        public static IServiceCollection AddStorageHandler(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddScoped<IAmazonS3>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<StorageClientConfiguration>();
                var s3Configuration = new AmazonS3Config
                {
                    ServiceURL = configuration.ServiceUrl,
                    ForcePathStyle = configuration.ForcePathStyle,
                    UseHttp = configuration.UseHttp,
                };
                return new AmazonS3Client(configuration.AccessKey, configuration.SecretAccessKey, s3Configuration);
            } );

            serviceDescriptors.AddScoped<IStorageHandler, StorageHandler>();

            return serviceDescriptors;
        }
    }
}