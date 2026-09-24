using MassTransit;
using MassTransit.Courier.Contracts;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Activities.Pictures;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;
using Musify.Infrastructure.MassTransit.RoutingSlip;
using Musify.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Musify.Infrastructure.Tests.MassTransit
{
    /// <summary>Runs the picture-update activities inside real routing slips on the MassTransit test harness.</summary>
    [Collection(InfrastructureCollection.Name)]
    public sealed class UpdatePicturesActivityTests(InfrastructureTestFixture fixture)
    {
        private const string SmallVariable = "Picture.Small";
        private const string MediumVariable = "Picture.Medium";
        private const string LargeVariable = "Picture.Large";

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task Album_Execute_StoresTheNamesAndPublishesTheEvent()
        {
            var album = await SeedAlbumAsync(EntityPictures.Pending("old-original.webp"));
            await using var provider = CreateProvider();
            var harness = await StartAsync(provider);

            var slip = SlipFor(UpdateAlbumPictureActivity.ExecuteEndpointName, album.Id, "uploads/1/albums/new-original.webp");
            await harness.Bus.Execute(slip.Build(), Ct);

            Assert.True(await harness.Sent.Any<RoutingSlipCompleted>(Ct));
            await using var verifier = fixture.CreateDatabase();
            var stored = (await verifier.Albums.AsNoTracking().SingleAsync(a => a.Id == album.Id, Ct)).Pictures!;
            Assert.Equal("new-original.webp", stored.OriginalName);
            Assert.Equal("small.webp", stored.SmallName);
            Assert.Equal("medium.webp", stored.MediumName);
            Assert.Equal("large.webp", stored.LargeName);
            Assert.True(await harness.Published.Any<AlbumPictureProcessed>(published => published.Context.Message.AlbumId == album.Id, Ct));
        }

        [Fact]
        public async Task PlayList_Execute_StoresTheNamesAndPublishesTheEvent()
        {
            var playList = await SeedPlayListAsync(EntityPictures.Pending("old.webp"));
            await using var provider = CreateProvider();
            var harness = await StartAsync(provider);

            var slip = SlipFor(UpdatePlayListPictureActivity.ExecuteEndpointName, playList.Id, "uploads/1/playlists/new.webp");
            await harness.Bus.Execute(slip.Build(), Ct);

            Assert.True(await harness.Sent.Any<RoutingSlipCompleted>(Ct));
            await using var verifier = fixture.CreateDatabase();
            var stored = (await verifier.PlayLists.AsNoTracking().SingleAsync(p => p.Id == playList.Id, Ct)).Pictures!;
            Assert.Equal("new.webp", stored.OriginalName);
            Assert.Equal("small.webp", stored.SmallName);
            Assert.True(await harness.Published.Any<PlayListPictureProcessed>(published => published.Context.Message.PlayListId == playList.Id, Ct));
        }

        [Fact]
        public async Task Track_Execute_MarksThePicturesAsCompleted()
        {
            var track = await SeedTrackAsync();
            await using var provider = CreateProvider();
            var harness = await StartAsync(provider);

            var slip = SlipFor(UpdateTrackPictureActivity.ExecuteEndpointName, track.Id, "uploads/1/tracks/new.webp");
            await harness.Bus.Execute(slip.Build(), Ct);

            Assert.True(await harness.Sent.Any<RoutingSlipCompleted>(Ct));
            await using var verifier = fixture.CreateDatabase();
            var stored = (await verifier.Tracks.AsNoTracking().SingleAsync(t => t.Id == track.Id, Ct)).Pictures;
            Assert.Equal(ProcessingStatus.Completed, stored.ProcessingStatus);
            Assert.Equal("large.webp", stored.LargeName);
            Assert.True(await harness.Published.Any<TrackPictureProcessed>(published => published.Context.Message.TrackId == track.Id, Ct));
        }

        [Fact]
        public async Task Execute_MissingEntity_FaultsTheSlip()
        {
            await using var provider = CreateProvider();
            var harness = await StartAsync(provider);

            var slip = SlipFor(UpdateAlbumPictureActivity.ExecuteEndpointName, Guid.NewGuid(), "uploads/1/albums/x.webp");
            await harness.Bus.Execute(slip.Build(), Ct);

            Assert.True(await harness.Sent.Any<RoutingSlipFaulted>(Ct));
        }

        [Fact]
        public async Task Execute_MissingVariable_FaultsTheSlip()
        {
            var album = await SeedAlbumAsync(EntityPictures.Pending("old.webp"));
            await using var provider = CreateProvider();
            var harness = await StartAsync(provider);

            var slip = RoutingSlips.Create(null).AddStep(
                "Update",
                UpdateAlbumPictureActivity.ExecuteEndpointName,
                new UpdatePicturesArguments(album.Id, "key.webp", SmallVariable, MediumVariable, LargeVariable));
            await harness.Bus.Execute(slip.Build(), Ct);

            Assert.True(await harness.Sent.Any<RoutingSlipFaulted>(Ct));
            await using var verifier = fixture.CreateDatabase();
            Assert.Equal("old.webp", (await verifier.Albums.AsNoTracking().SingleAsync(a => a.Id == album.Id, Ct)).Pictures!.OriginalName);
        }

        [Fact]
        public async Task Album_LaterStepFails_CompensationRestoresThePreviousNames()
        {
            var album = await SeedAlbumAsync(EntityPictures.Pending("old-original.webp"));
            await using var provider = CreateProvider();
            var harness = await StartAsync(provider);

            var slip = SlipFor(UpdateAlbumPictureActivity.ExecuteEndpointName, album.Id, "uploads/1/albums/new-original.webp");
            slip.AddStep("Fail", AlwaysFailsActivity.ExecuteEndpointName, new AlwaysFailsArguments());
            await harness.Bus.Execute(slip.Build(), Ct);

            Assert.True(await harness.Sent.Any<RoutingSlipFaulted>(Ct));
            await WaitUntilAsync(async () =>
            {
                await using var poll = fixture.CreateDatabase();
                return (await poll.Albums.AsNoTracking().SingleAsync(a => a.Id == album.Id, Ct)).Pictures!.OriginalName == "old-original.webp";
            });
            await using var verifier = fixture.CreateDatabase();
            var stored = (await verifier.Albums.AsNoTracking().SingleAsync(a => a.Id == album.Id, Ct)).Pictures!;
            Assert.Equal("old-original.webp", stored.OriginalName);
            Assert.Null(stored.SmallName);
            Assert.Null(stored.MediumName);
            Assert.Null(stored.LargeName);
        }

        [Fact]
        public async Task Track_LaterStepFails_CompensationMarksThePicturesAsFailed()
        {
            var track = await SeedTrackAsync();
            await using var provider = CreateProvider();
            var harness = await StartAsync(provider);

            var slip = SlipFor(UpdateTrackPictureActivity.ExecuteEndpointName, track.Id, "uploads/1/tracks/new.webp");
            slip.AddStep("Fail", AlwaysFailsActivity.ExecuteEndpointName, new AlwaysFailsArguments());
            await harness.Bus.Execute(slip.Build(), Ct);

            Assert.True(await harness.Sent.Any<RoutingSlipFaulted>(Ct));
            await WaitUntilAsync(async () =>
            {
                await using var poll = fixture.CreateDatabase();
                return (await poll.Tracks.AsNoTracking().SingleAsync(t => t.Id == track.Id, Ct)).Pictures.ProcessingStatus == ProcessingStatus.Failed;
            });
            await using var verifier = fixture.CreateDatabase();
            var stored = (await verifier.Tracks.AsNoTracking().SingleAsync(t => t.Id == track.Id, Ct)).Pictures;
            Assert.Equal(ProcessingStatus.Failed, stored.ProcessingStatus);
            Assert.Equal("old.webp", stored.OriginalName);
        }

        private static async Task WaitUntilAsync(Func<Task<bool>> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!await condition() && DateTime.UtcNow < deadline)
                await Task.Delay(100, Ct);
        }

        private static RoutingSlipBuilder SlipFor(string endpointName, Guid subjectId, string originalKey)
        {
            var builder = RoutingSlips.Create(null).AddStep(
                "Update", endpointName, new UpdatePicturesArguments(subjectId, originalKey, SmallVariable, MediumVariable, LargeVariable));
            builder.AddVariable(SmallVariable, "pictures/small.webp");
            builder.AddVariable(MediumVariable, "pictures/medium.webp");
            builder.AddVariable(LargeVariable, "pictures/large.webp");
            return builder;
        }

        private ServiceProvider CreateProvider() =>
            new ServiceCollection()
                .AddScoped<IDatabase>(_ => fixture.CreateDatabase())
                .AddMassTransitTestHarness(bus =>
                {
                    bus.SetKebabCaseEndpointNameFormatter();
                    bus.AddActivity<UpdateAlbumPictureActivity, UpdatePicturesArguments, UpdatePicturesLog>();
                    bus.AddActivity<UpdatePlayListPictureActivity, UpdatePicturesArguments, UpdatePicturesLog>();
                    bus.AddActivity<UpdateTrackPictureActivity, UpdatePicturesArguments, UpdatePicturesLog>();
                    bus.AddExecuteActivity<AlwaysFailsActivity, AlwaysFailsArguments>();
                })
                .BuildServiceProvider(true);

        private static async Task<ITestHarness> StartAsync(ServiceProvider provider)
        {
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();
            return harness;
        }

        private async Task<User> SeedUserAsync()
        {
            await using var database = fixture.CreateDatabase();
            var name = $"pictures-{Guid.NewGuid():N}"[..30];
            var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = name, NormalizedName = name.ToUpperInvariant() };
            await database.Users.AddAsync(user, Ct);
            await database.SaveChangesAsync(Ct);
            return user;
        }

        private async Task<Album> SeedAlbumAsync(EntityPictures pictures)
        {
            var user = await SeedUserAsync();
            await using var database = fixture.CreateDatabase();
            var title = $"album-{Guid.NewGuid():N}";
            var album = new Album { Title = title, NormalizedTitle = title.ToUpperInvariant(), OwnerUserId = user.Id, Pictures = pictures };
            await database.Albums.AddAsync(album, Ct);
            await database.SaveChangesAsync(Ct);
            return album;
        }

        private async Task<PlayList> SeedPlayListAsync(EntityPictures pictures)
        {
            var user = await SeedUserAsync();
            await using var database = fixture.CreateDatabase();
            var name = $"pl-{Guid.NewGuid():N}"[..30];
            var playList = new PlayList { Name = name, NormalizedName = name.ToUpperInvariant(), OwnerUserId = user.Id, Pictures = pictures };
            await database.PlayLists.AddAsync(playList, Ct);
            await database.SaveChangesAsync(Ct);
            return playList;
        }

        private async Task<Track> SeedTrackAsync()
        {
            var user = await SeedUserAsync();
            await using var database = fixture.CreateDatabase();
            var title = $"track-{Guid.NewGuid():N}";
            var track = new Track
            {
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                DurationSeconds = 10,
                OwnerUserId = user.Id,
                Pictures = new TrackPictures { OriginalName = "old.webp" },
                Audio = new TrackAudio()
            };
            await database.Tracks.AddAsync(track, Ct);
            await database.SaveChangesAsync(Ct);
            return track;
        }

        public sealed record AlwaysFailsArguments;

        public sealed class AlwaysFailsActivity : IExecuteActivity<AlwaysFailsArguments>
        {
            public const string ExecuteEndpointName = "always-fails";

            public Task<ExecutionResult> Execute(ExecuteContext<AlwaysFailsArguments> context) =>
                throw new InvalidOperationException("Forced failure");
        }
    }
}
