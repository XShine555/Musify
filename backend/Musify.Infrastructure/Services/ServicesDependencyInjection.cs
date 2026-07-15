using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Musify.Application.Configuration;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Jobs;
using Musify.Application.Contracts;

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

            serviceDescriptors.AddSingleton<IAmazonS3>(serviceProvider =>
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

        public static IServiceCollection AddUploadIntentConfiguration(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddValidatedOptions<UploadIntentConfiguration>(configuration, UploadIntentConfiguration.SectionName);

            return serviceDescriptors;
        }

        public static IServiceCollection AddUploadIntentJobs(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddUploadIntentConfiguration(configuration);

            serviceDescriptors.AddHostedService<UploadIntentExpirationJob>();
            serviceDescriptors.AddHostedService<TemporalUploadsCleanUpJob>();

            return serviceDescriptors;
        }

        public static IServiceCollection AddStreamTicketService(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<StreamTicketConfiguration>()
                .Bind(configuration.GetRequiredSection(StreamTicketConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<StreamTicketConfiguration>>().Value);

            serviceDescriptors.AddSingleton<IStreamTicketService, StreamTicketService>();
            return serviceDescriptors;
        }

        public static IServiceCollection AddYouTubeMusicService(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddValidatedOptions<YouTubeConfiguration>(configuration, YouTubeConfiguration.SectionName);

            serviceDescriptors.AddMemoryCache();
            serviceDescriptors.AddSingleton<IYouTubeMusicService, YouTubeMusicService>();
            return serviceDescriptors;
        }

        public static IServiceCollection AddYouTubeDownloader(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddValidatedOptions<YtDlpConfiguration>(configuration, YtDlpConfiguration.SectionName);

            serviceDescriptors.AddScoped<IYouTubeDownloaderService, YouTubeDownloaderService>();
            return serviceDescriptors;
        }

        public static IServiceCollection AddAudioTranscoder(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors
                .AddOptionsWithValidateOnStart<AudioConfiguration>()
                .Bind(configuration.GetRequiredSection(AudioConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<AudioConfiguration>>().Value);

            serviceDescriptors.AddScoped<IAudioTranscoderService, AudioService>();
            return serviceDescriptors;
        }
    }
}