using Amazon.S3;
using Keycloak.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Musify.Application.Contracts.Infrastructure;
using Musify.Infrastructure.Configuration;

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

        public static IServiceCollection AddKeycloakService(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddOptionsWithValidateOnStart<KeycloakConfiguration>()
                .Bind(configuration.GetRequiredSection(KeycloakConfiguration.SectionName))
                .ValidateDataAnnotations();

            serviceDescriptors.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<KeycloakConfiguration>>().Value);

            serviceDescriptors.AddScoped(serviceProvider =>
            {
                var keycloakConfiguration = serviceProvider.GetRequiredService<KeycloakConfiguration>();

                return new KeycloakClient(
                    keycloakConfiguration.Address,
                    keycloakConfiguration.Username,
                    keycloakConfiguration.Password);
            } );
            serviceDescriptors.AddScoped<IKeycloakUserService, KeycloakUserService>();
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