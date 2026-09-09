using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.Services
{
    public class PlayListPresetSeeder(IServiceScopeFactory scopeFactory, ILogger<PlayListPresetSeeder> logger)
        : BackgroundService
    {
        private const string SourceResourceName = "Musify.Infrastructure.Assets.PlayListDefaultCover.png";
        private const string WebpContentType = "image/webp";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = scopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            var playListConfiguration = services.GetRequiredService<PlayListConfiguration>();
            if (!playListConfiguration.SeedPresetPictures)
                return;

            try
            {
                await SeedAsync(services, playListConfiguration, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to seed playlist preset pictures");
            }
        }

        private async Task SeedAsync(IServiceProvider services, PlayListConfiguration configuration, CancellationToken cancellationToken)
        {
            var storage = services.GetRequiredService<IStorageService>();
            var pictures = services.GetRequiredService<IPictureService>();
            var bucket = services.GetRequiredService<ApplicationStorageConfiguration>().Bucket;

            var source = LoadSourceBytes();

            var routes = configuration.Routes;
            var sizes = configuration.PicturesSizes;

            var targets = new (string Key, int Width, int Height)[]
            {
                (routes.BuildOriginalPicturePath(routes.PresetOriginalPicture), sizes.LargePictureWidth, sizes.LargePictureHeight),
                (routes.PresetSmallPicturePath, sizes.SmallPictureWidth, sizes.SmallPictureHeight),
                (routes.PresetMediumPicturePath, sizes.MediumPictureWidth, sizes.MediumPictureHeight),
                (routes.PresetLargePicturePath, sizes.LargePictureWidth, sizes.LargePictureHeight),
            };

            foreach (var target in targets)
            {
                if (!configuration.OverwritePresetPictures)
                {
                    var existing = await storage.HeadObjectAsync(bucket, target.Key, cancellationToken);
                    if (existing is not null)
                    {
                        logger.LogDebug("Playlist preset picture already present at {Bucket}/{Key}", bucket, target.Key);
                        continue;
                    }
                }

                using var sourceStream = new MemoryStream(source, writable: false);
                await using var resized = await pictures.ResizePictureAsWebpAsync(sourceStream, target.Width, target.Height, cancellationToken);
                await storage.UploadFileAsync(resized, WebpContentType, bucket, target.Key, cancellationToken);

                logger.LogInformation("Seeded playlist preset picture {Bucket}/{Key} ({Width}x{Height})", bucket, target.Key, target.Width, target.Height);
            }
        }

        private static byte[] LoadSourceBytes()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(SourceResourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{SourceResourceName}' was not found.");

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
