using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Configuration;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Jobs;

namespace Musify.Infrastructure.Services
{
    public static class ServicesDependencyInjection
    {
        public static IServiceCollection AddStorageService(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<InfrastructureStorageConfiguration>()
                .Bind(configuration.GetRequiredSection(InfrastructureStorageConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<InfrastructureStorageConfiguration>>().Value);

            serviceDescriptors.AddScoped<IAmazonS3>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<InfrastructureStorageConfiguration>();
                var s3Configuration = new AmazonS3Config
                {
                    ServiceURL = configuration.Address,
                    ForcePathStyle = configuration.ForcePathStyle,
                    UseHttp = configuration.UseHttp,
                };
                return new AmazonS3Client(configuration.AccessKey, configuration.SecretAccessKey, s3Configuration);
            } );

            serviceDescriptors.AddScoped<IStorageService, StorageService>();
            return serviceDescriptors;
        }

        public static IServiceCollection AddPictureService(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddScoped<IPictureService, PictureService>();
            return serviceDescriptors;
        }

        // Called by both the web API host and the worker
        public static IServiceCollection AddUploadIntentConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<UploadIntentConfiguration>()
                .Bind(configuration.GetRequiredSection(UploadIntentConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<UploadIntentConfiguration>>().Value);

            return serviceDescriptors;
        }

        // Called only by the worker host (registers background jobs)
        public static IServiceCollection AddUploadIntentJobs(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddUploadIntentConfiguration(configuration);

            serviceDescriptors.AddHostedService<UploadIntentExpirationJob>();
            serviceDescriptors.AddHostedService<TempUploadsCleanupJob>();

            return serviceDescriptors;
        }

        public static IServiceCollection AddAudioTranscoder(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<AudioTranscoderConfiguration>()
                .Bind(configuration.GetRequiredSection(AudioTranscoderConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<AudioTranscoderConfiguration>>().Value);

            serviceDescriptors.AddScoped<IAudioTranscoderService, AudioTranscoderService>();
            return serviceDescriptors;
        }
    }
}