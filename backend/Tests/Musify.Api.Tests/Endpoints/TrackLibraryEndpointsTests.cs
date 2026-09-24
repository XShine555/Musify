using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.Tests.TestSupport;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Persistence;
using Xunit;

namespace Musify.Api.Tests.Endpoints
{
    [Collection(ApiCollection.Name)]
    public sealed class TrackLibraryEndpointsTests(ApiTestFixture fixture)
    {
        private async Task<Guid> SeedTrackAsync(
            long ownerId,
            string title,
            ProcessingStatus status = ProcessingStatus.Completed)
        {
            using var scope = fixture.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            var track = new Track
            {
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                OwnerUserId = ownerId,
                Audio = new TrackAudio { TranscodeStatus = status, FolderName = status == ProcessingStatus.Completed ? "folder" : null }
            };
            database.Tracks.Add(track);
            await database.SaveChangesAsync(TestContext.Current.CancellationToken);
            return track.Id;
        }

        private async Task<Guid> SeedAlbumWithTracksAsync(long ownerId, params Guid[] trackIds)
        {
            using var scope = fixture.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            var album = new Album { Title = "Album", NormalizedTitle = "ALBUM", OwnerUserId = ownerId };
            database.Albums.Add(album);
            var number = 1;
            foreach (var trackId in trackIds)
                database.AlbumHasTracks.Add(new AlbumHasTrack { AlbumId = album.Id, TrackId = trackId, TrackNumber = number++ });
            await database.SaveChangesAsync(TestContext.Current.CancellationToken);
            return album.Id;
        }

        private static int Number(JsonElement element) =>
            element.ValueKind == JsonValueKind.String
                ? int.Parse(element.GetString()!, CultureInfo.InvariantCulture)
                : element.GetInt32();

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        [Fact]
        public async Task GetUserTracks_StatusFilterBindsRepeatedQueryValues()
        {
            var userId = await fixture.SeedUserAsync();
            await SeedTrackAsync(userId, "Done");
            await SeedTrackAsync(userId, "Pending", ProcessingStatus.Pending);
            await SeedTrackAsync(userId, "Failed", ProcessingStatus.Failed);

            var response = await fixture.CreateAnonymousClient().GetAsync(
                $"/tracks/users/{userId.ToString(CultureInfo.InvariantCulture)}?status=Pending&status=Failed", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var titles = body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()).Order().ToList();
            Assert.Equal(["Failed", "Pending"], titles);
            Assert.Equal(2, Number(body.GetProperty("totalItemCount")));
        }

        [Fact]
        public async Task GetUserTracks_ReturnsAlbumsOfEachTrack()
        {
            var userId = await fixture.SeedUserAsync();
            var trackId = await SeedTrackAsync(userId, "In album");
            var albumId = await SeedAlbumWithTracksAsync(userId, trackId);

            var response = await fixture.CreateAnonymousClient().GetAsync(
                $"/tracks/users/{userId.ToString(CultureInfo.InvariantCulture)}", TestContext.Current.CancellationToken);

            var body = await ReadJsonAsync(response);
            var albums = body.GetProperty("items")[0].GetProperty("albums");
            Assert.Equal(1, albums.GetArrayLength());
            Assert.Equal(albumId, albums[0].GetProperty("id").GetGuid());
            Assert.Equal("Album", albums[0].GetProperty("title").GetString());
        }

        [Fact]
        public async Task GetStatusCounts_Owner_ReturnsCountsPerStatus()
        {
            var userId = await fixture.SeedUserAsync();
            await SeedTrackAsync(userId, "A");
            await SeedTrackAsync(userId, "B", ProcessingStatus.Processing);
            await SeedTrackAsync(userId, "C", ProcessingStatus.Failed);

            var response = await fixture.CreateAuthenticatedClient(userId).GetAsync(
                $"/tracks/users/{userId.ToString(CultureInfo.InvariantCulture)}/status-counts", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(3, Number(body.GetProperty("total")));
            Assert.Equal(1, Number(body.GetProperty("completed")));
            Assert.Equal(1, Number(body.GetProperty("processing")));
            Assert.Equal(1, Number(body.GetProperty("failed")));
            Assert.Equal(0, Number(body.GetProperty("pending")));
        }

        [Fact]
        public async Task GetStatusCounts_AnotherUser_ReturnsForbidden()
        {
            var owner = await fixture.SeedUserAsync();
            var stranger = await fixture.SeedUserAsync();

            var response = await fixture.CreateAuthenticatedClient(stranger).GetAsync(
                $"/tracks/users/{owner.ToString(CultureInfo.InvariantCulture)}/status-counts", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetStatusCounts_Anonymous_ReturnsUnauthorized()
        {
            var response = await fixture.CreateAnonymousClient().GetAsync("/tracks/users/1/status-counts", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAlbumTrackIds_Owner_ReturnsIdsInOrder()
        {
            var userId = await fixture.SeedUserAsync();
            var first = await SeedTrackAsync(userId, "First");
            var second = await SeedTrackAsync(userId, "Second");
            var albumId = await SeedAlbumWithTracksAsync(userId, second, first);

            var response = await fixture.CreateAuthenticatedClient(userId).GetAsync($"/albums/{albumId}/tracks/ids", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var ids = body.GetProperty("trackIds").EnumerateArray().Select(e => e.GetGuid()).ToList();
            Assert.Equal([second, first], ids);
        }

        [Fact]
        public async Task GetAlbumTrackIds_NotTheOwner_ReturnsForbidden()
        {
            var owner = await fixture.SeedUserAsync();
            var stranger = await fixture.SeedUserAsync();
            var albumId = await SeedAlbumWithTracksAsync(owner);

            var response = await fixture.CreateAuthenticatedClient(stranger).GetAsync($"/albums/{albumId}/tracks/ids", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTracks_Anonymous_ReturnsUnauthorized()
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "/tracks") { Content = JsonContent.Create(new DeleteTracksRequest([Guid.NewGuid()])) };

            var response = await fixture.CreateAnonymousClient().SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTracks_EmptyList_ReturnsBadRequest()
        {
            var userId = await fixture.SeedUserAsync();
            var request = new HttpRequestMessage(HttpMethod.Delete, "/tracks") { Content = JsonContent.Create(new DeleteTracksRequest([])) };

            var response = await fixture.CreateAuthenticatedClient(userId).SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTracks_OwnTracks_MarksThemRemovingAndReportsFailures()
        {
            var userId = await fixture.SeedUserAsync();
            var done = await SeedTrackAsync(userId, "Done");
            var busy = await SeedTrackAsync(userId, "Busy", ProcessingStatus.Pending);
            var request = new HttpRequestMessage(HttpMethod.Delete, "/tracks") { Content = JsonContent.Create(new DeleteTracksRequest([done, busy])) };

            var response = await fixture.CreateAuthenticatedClient(userId).SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal([done], body.GetProperty("deletedTrackIds").EnumerateArray().Select(e => e.GetGuid()).ToList());
            Assert.Equal([busy], body.GetProperty("failedTrackIds").EnumerateArray().Select(e => e.GetGuid()).ToList());

            using var scope = fixture.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            var stored = await database.Tracks.AsNoTracking().SingleAsync(t => t.Id == done, TestContext.Current.CancellationToken);
            Assert.Equal(LifeCycleStatus.Removing, stored.LifeCycleStatus);
        }

        [Fact]
        public async Task DeleteTracks_TrackOfAnotherUser_ReturnsForbidden()
        {
            var owner = await fixture.SeedUserAsync();
            var stranger = await fixture.SeedUserAsync();
            var trackId = await SeedTrackAsync(owner, "Theirs");
            var request = new HttpRequestMessage(HttpMethod.Delete, "/tracks") { Content = JsonContent.Create(new DeleteTracksRequest([trackId])) };

            var response = await fixture.CreateAuthenticatedClient(stranger).SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
