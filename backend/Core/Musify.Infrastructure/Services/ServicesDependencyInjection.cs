using Amazon.S3;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Jobs;

namespace Musify.Infrastructure.Services
{
    public static class ServicesDependencyInjection
    {
        public static IServiceCollection AddStorageService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatedOptions<InfrastructureStorageConfiguration>(configuration);

            services.AddSingleton<IAmazonS3>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<InfrastructureStorageConfiguration>();
                var s3Configuration = new AmazonS3Config
                {
                    ServiceURL = configuration.Address,
                    ForcePathStyle = configuration.ForcePathStyle,
                    UseHttp = configuration.UseHttp,
                };
                return new AmazonS3Client(configuration.AccessKey, configuration.SecretAccessKey, s3Configuration);
            });

            services.AddSingleton<IStorageService, StorageService>();
            return services;
        }

        public static IServiceCollection AddPictureService(this IServiceCollection services)
        {
            services.AddScoped<IPictureService, PictureService>();
            return services;
        }

        public static IServiceCollection AddJobs(this IServiceCollection services)
        {
            services.AddHangfire((serviceProvider, hangfireConfiguration) =>
            {
                var databaseConfiguration = serviceProvider.GetRequiredService<DatabaseConfiguration>();
                hangfireConfiguration.UsePostgreSqlStorage(options =>
                    options.UseNpgsqlConnection(databaseConfiguration.ConnectionString));
            });

            services.AddHangfireServer();
            services.AddScoped<DailyMixGenerationJob>();
            services.AddScoped<ListeningHistoryCleanupJob>();
            services.AddScoped<UploadIntentExpirationJob>();
            services.AddScoped<TempUploadsCleanupJob>();
            services.AddHostedService<RecurringJobsRegistrar>();

            return services;
        }

        public static IServiceCollection AddStreamTicketService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatedOptions<StreamTicketConfiguration>(configuration);

            services.AddSingleton<IStreamTicketService, StreamTicketService>();
            return services;
        }

        public static IServiceCollection AddAudioTranscoder(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatedOptions<AudioConfiguration>(configuration);

            services.AddScoped<IAudioTranscoderService, AudioTranscoderService>();
            return services;
        }
    }
}
