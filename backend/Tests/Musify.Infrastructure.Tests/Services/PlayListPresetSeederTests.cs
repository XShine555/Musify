using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.Services;
using Musify.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Musify.Infrastructure.Tests.Services
{
    [Collection(InfrastructureCollection.Name)]
    public sealed class PlayListPresetSeederTests(InfrastructureTestFixture fixture)
    {
        private static PlayListConfiguration NewConfiguration(bool overwritePresetPictures) => new()
        {
            Routes = new PlayListRoutes { ParentFolders = $"PlayLists-{Guid.NewGuid():N}" },
            SeedPresetPictures = true,
            OverwritePresetPictures = overwritePresetPictures
        };

        private (PlayListPresetSeeder Seeder, IStorageService Storage) Build(PlayListConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddSingleton(configuration);
            services.AddSingleton(new ApplicationStorageConfiguration { Bucket = InfrastructureTestFixture.S3Bucket });
            services.AddSingleton<IStorageService>(_ => new StorageService(
                fixture.CreateDatabase(),
                fixture.CreateS3Client(),
                NullLogger<StorageService>.Instance,
                new InfrastructureStorageConfiguration
                {
                    Address = fixture.S3ServiceUrl,
                    AccessKey = InfrastructureTestFixture.S3AccessKey,
                    SecretAccessKey = InfrastructureTestFixture.S3SecretKey,
                    ForcePathStyle = true,
                    UseHttp = true,
                }));
            services.AddSingleton<IPictureService>(new PictureService(NullLogger<PictureService>.Instance));

            var provider = services.BuildServiceProvider();
            var seeder = new PlayListPresetSeeder(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<PlayListPresetSeeder>.Instance);
            return (seeder, provider.GetRequiredService<IStorageService>());
        }

        private static async Task RunToCompletionAsync(PlayListPresetSeeder seeder)
        {
            await seeder.StartAsync(TestContext.Current.CancellationToken);
            await (seeder.ExecuteTask ?? Task.CompletedTask);
        }

        [Fact]
        public async Task ExecuteAsync_SeedsAllFourPresetPictures()
        {
            var configuration = NewConfiguration(overwritePresetPictures: false);
            var (seeder, storage) = Build(configuration);

            await RunToCompletionAsync(seeder);

            var routes = configuration.Routes;
            foreach (var key in new[]
                     {
                         routes.BuildOriginalPicturePath(routes.PresetOriginalPicture),
                         routes.PresetSmallPicturePath,
                         routes.PresetMediumPicturePath,
                         routes.PresetLargePicturePath
                     })
            {
                var metadata = await storage.HeadObjectAsync(InfrastructureTestFixture.S3Bucket, key, TestContext.Current.CancellationToken);
                Assert.True(metadata is not null, $"expected {key} to have been seeded");
            }
        }

        [Fact]
        public async Task ExecuteAsync_RunTwiceWithoutOverwrite_StaysIdempotent()
        {
            var configuration = NewConfiguration(overwritePresetPictures: false);
            var (firstSeeder, storage) = Build(configuration);
            await RunToCompletionAsync(firstSeeder);

            var (secondSeeder, _) = Build(configuration);
            await RunToCompletionAsync(secondSeeder);

            var metadata = await storage.HeadObjectAsync(
                InfrastructureTestFixture.S3Bucket, configuration.Routes.PresetSmallPicturePath, TestContext.Current.CancellationToken);
            Assert.NotNull(metadata);
        }

        [Fact]
        public async Task ExecuteAsync_SeedPresetPicturesDisabled_UploadsNothing()
        {
            var configuration = NewConfiguration(overwritePresetPictures: false);
            configuration.SeedPresetPictures = false;
            var (seeder, storage) = Build(configuration);

            await RunToCompletionAsync(seeder);

            var metadata = await storage.HeadObjectAsync(
                InfrastructureTestFixture.S3Bucket, configuration.Routes.PresetSmallPicturePath, TestContext.Current.CancellationToken);
            Assert.Null(metadata);
        }
    }
}
