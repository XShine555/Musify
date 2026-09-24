using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Musify.Api.DataTransferObjects;
using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.Tests.TestSupport;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Persistence;
using NSubstitute;
using Xunit;

namespace Musify.Api.Tests.Endpoints
{
    [Collection(ApiCollection.Name)]
    public sealed class TrackPublicationEndpointsTests(ApiTestFixture fixture)
    {
        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private async Task<Guid> SeedTrackAsync(
            long ownerId,
            string title,
            ProcessingStatus status = ProcessingStatus.Completed,
            bool isPublished = true)
        {
            using var scope = fixture.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            var track = new Track
            {
                Title = title,
                NormalizedTitle = title.ToUpperInvariant(),
                OwnerUserId = ownerId,
                IsPublished = isPublished,
                Audio = new TrackAudio { TranscodeStatus = status, FolderName = status == ProcessingStatus.Completed ? "folder" : null }
            };
            database.Tracks.Add(track);
            await database.SaveChangesAsync(Ct);
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
            await database.SaveChangesAsync(Ct);
            return album.Id;
        }

        private void MockStorage()
        {
            fixture.StorageService
                .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns("https://storage.musify.test/presigned-upload");
            fixture.StorageService
                .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ObjectMetaData("image/webp", 1024));
        }

        private static int Number(JsonElement element) =>
            element.ValueKind == JsonValueKind.String
                ? int.Parse(element.GetString()!, CultureInfo.InvariantCulture)
                : element.GetInt32();

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync(Ct);
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        private static string UserPath(long userId) => $"/tracks/users/{userId.ToString(CultureInfo.InvariantCulture)}";

        [Fact]
        public async Task PutPublished_Owner_PublishesTheTrackForEveryone()
        {
            var userId = await fixture.SeedUserAsync();
            var trackId = await SeedTrackAsync(userId, "Hidden", isPublished: false);
            var anonymous = fixture.CreateAnonymousClient();
            var before = await anonymous.GetAsync($"/tracks/{trackId}", Ct);

            var publish = await fixture.CreateAuthenticatedClient(userId)
                .PutAsJsonAsync($"/tracks/{trackId}/published", new SetTrackPublishedRequest(true), Ct);
            var after = await anonymous.GetAsync($"/tracks/{trackId}", Ct);

            Assert.Equal(HttpStatusCode.NotFound, before.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, publish.StatusCode);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
            Assert.True((await ReadJsonAsync(after)).GetProperty("isPublished").GetBoolean());
        }

        [Fact]
        public async Task PutPublished_AudioNotProcessed_ReturnsBadRequest()
        {
            var userId = await fixture.SeedUserAsync();
            var trackId = await SeedTrackAsync(userId, "Busy", ProcessingStatus.Processing, isPublished: false);

            var response = await fixture.CreateAuthenticatedClient(userId)
                .PutAsJsonAsync($"/tracks/{trackId}/published", new SetTrackPublishedRequest(true), Ct);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutPublished_NotTheOwnerOrAnonymous_IsRejected()
        {
            var owner = await fixture.SeedUserAsync();
            var stranger = await fixture.SeedUserAsync();
            var trackId = await SeedTrackAsync(owner, "Mine", isPublished: false);

            var asStranger = await fixture.CreateAuthenticatedClient(stranger)
                .PutAsJsonAsync($"/tracks/{trackId}/published", new SetTrackPublishedRequest(true), Ct);
            var anonymous = await fixture.CreateAnonymousClient()
                .PutAsJsonAsync($"/tracks/{trackId}/published", new SetTrackPublishedRequest(true), Ct);

            Assert.Equal(HttpStatusCode.Forbidden, asStranger.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        }

        [Fact]
        public async Task PutPublishedBatch_UpdatesProcessedTracksAndReportsTheSkippedOnes()
        {
            var userId = await fixture.SeedUserAsync();
            var ready = await SeedTrackAsync(userId, "Ready", isPublished: false);
            var busy = await SeedTrackAsync(userId, "Busy", ProcessingStatus.Pending, isPublished: false);

            var response = await fixture.CreateAuthenticatedClient(userId)
                .PutAsJsonAsync("/tracks/published", new SetTracksPublishedRequest([ready, busy], true), Ct);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal([ready], body.GetProperty("updatedTrackIds").EnumerateArray().Select(e => e.GetGuid()).ToList());
            Assert.Equal([busy], body.GetProperty("skippedTrackIds").EnumerateArray().Select(e => e.GetGuid()).ToList());
        }

        [Fact]
        public async Task PutPublishedBatch_EmptyList_ReturnsBadRequest()
        {
            var userId = await fixture.SeedUserAsync();

            var response = await fixture.CreateAuthenticatedClient(userId)
                .PutAsJsonAsync("/tracks/published", new SetTracksPublishedRequest([], true), Ct);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetUserTracks_UnpublishedTracksOnlyReachTheOwner()
        {
            var userId = await fixture.SeedUserAsync();
            var stranger = await fixture.SeedUserAsync();
            await SeedTrackAsync(userId, "Live");
            await SeedTrackAsync(userId, "Hidden", isPublished: false);
            var path = UserPath(userId);

            var anonymous = await ReadJsonAsync(await fixture.CreateAnonymousClient().GetAsync(path, Ct));
            var asStranger = await ReadJsonAsync(await fixture.CreateAuthenticatedClient(stranger).GetAsync(path, Ct));
            var asOwner = await ReadJsonAsync(await fixture.CreateAuthenticatedClient(userId).GetAsync(path, Ct));
            var onlyHidden = await ReadJsonAsync(await fixture.CreateAuthenticatedClient(userId).GetAsync($"{path}?published=false", Ct));

            Assert.Equal(1, Number(anonymous.GetProperty("totalItemCount")));
            Assert.Equal(1, Number(asStranger.GetProperty("totalItemCount")));
            Assert.Equal(2, Number(asOwner.GetProperty("totalItemCount")));
            Assert.Equal("Hidden", onlyHidden.GetProperty("items")[0].GetProperty("title").GetString());
            Assert.False(onlyHidden.GetProperty("items")[0].GetProperty("isPublished").GetBoolean());
        }

        [Fact]
        public async Task StatusCounts_SplitPublishedFromUnpublished()
        {
            var userId = await fixture.SeedUserAsync();
            await SeedTrackAsync(userId, "Live");
            await SeedTrackAsync(userId, "Hidden", isPublished: false);
            await SeedTrackAsync(userId, "Busy", ProcessingStatus.Pending, isPublished: false);

            var response = await fixture.CreateAuthenticatedClient(userId).GetAsync($"{UserPath(userId)}/status-counts", Ct);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, Number(body.GetProperty("published")));
            Assert.Equal(2, Number(body.GetProperty("unpublished")));
        }

        [Fact]
        public async Task Search_AndStream_IgnoreUnpublishedTracks()
        {
            var owner = await fixture.SeedUserAsync();
            var stranger = await fixture.SeedUserAsync();
            var title = $"Secret{Guid.NewGuid():N}";
            var trackId = await SeedTrackAsync(owner, title, isPublished: false);

            var search = await ReadJsonAsync(await fixture.CreateAnonymousClient().GetAsync($"/tracks?name={title}", Ct));
            var stream = await fixture.CreateAuthenticatedClient(stranger).GetAsync($"/tracks/{trackId}/stream", Ct);

            Assert.Equal(0, Number(search.GetProperty("totalItemCount")));
            Assert.Equal(HttpStatusCode.NotFound, stream.StatusCode);
        }

        [Fact]
        public async Task GetAlbum_HidesUnpublishedTracksFromTheListAndTheCount()
        {
            var userId = await fixture.SeedUserAsync();
            var live = await SeedTrackAsync(userId, "Live");
            var hidden = await SeedTrackAsync(userId, "Hidden", isPublished: false);
            var albumId = await SeedAlbumWithTracksAsync(userId, hidden, live);

            var tracks = await ReadJsonAsync(await fixture.CreateAnonymousClient().GetAsync($"/albums/{albumId}/tracks", Ct));
            var album = await ReadJsonAsync(await fixture.CreateAnonymousClient().GetAsync($"/albums/{albumId}", Ct));

            Assert.Equal(1, Number(tracks.GetProperty("totalItemCount")));
            Assert.Equal(1, Number(album.GetProperty("trackCount")));
            Assert.Equal("Album", album.GetProperty("type").GetString());
        }

        [Fact]
        public async Task PostUploadUrls_ManyAudiosAndSharedPicture_ReturnsAnIntentPerFile()
        {
            var userId = await fixture.SeedUserAsync();
            MockStorage();
            var request = new RequestTrackUploadUrlsRequest(
                "webp", "image/webp", Audios: [new("mp3", "audio/mpeg", 1000), new("flac", "audio/flac", 2000)]);

            var response = await fixture.CreateAuthenticatedClient(userId).PostAsJsonAsync("/tracks/upload-urls", request, Ct);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var audios = body.GetProperty("audios").EnumerateArray().ToList();
            Assert.Equal(2, audios.Count);
            Assert.All(audios, audio => Assert.NotEqual(JsonValueKind.Null, audio.GetProperty("pictureIntentId").ValueKind));
            Assert.Equal(2, audios.Select(audio => audio.GetProperty("intentId").GetGuid()).Distinct().Count());
            Assert.Equal(2, audios.Select(audio => audio.GetProperty("pictureIntentId").GetGuid()).Distinct().Count());
        }

        [Fact]
        public async Task PostUploadUrls_AudiosOnly_HasNoPictureIntent()
        {
            var userId = await fixture.SeedUserAsync();
            MockStorage();

            var response = await fixture.CreateAuthenticatedClient(userId).PostAsJsonAsync(
                "/tracks/upload-urls", new RequestTrackUploadUrlsRequest(Audios: [new("mp3", "audio/mpeg")]), Ct);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("pictureIntentId").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("audios")[0].GetProperty("pictureIntentId").ValueKind);
        }

        [Fact]
        public async Task PostAlbum_WithTypeAndTracks_LinksThemAndPutChangesTheType()
        {
            var userId = await fixture.SeedUserAsync();
            var client = fixture.CreateAuthenticatedClient(userId);
            var first = await SeedTrackAsync(userId, "First");
            var second = await SeedTrackAsync(userId, "Second");
            MockStorage();
            var upload = await ReadJsonAsync(await client.PostAsJsonAsync("/albums/upload-picture", new PictureUploadRequest("webp", "image/webp"), Ct));
            var intentId = upload.GetProperty("intentId").GetGuid();

            var created = await client.PostAsJsonAsync(
                "/albums", new CreateAlbumRequest("Typed", null, 2024, intentId, AlbumType.EP, [second, first]), Ct);

            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var album = await ReadJsonAsync(created);
            Assert.Equal("EP", album.GetProperty("type").GetString());
            Assert.Equal(2, Number(album.GetProperty("trackCount")));
            var albumId = album.GetProperty("id").GetGuid();

            var ids = await ReadJsonAsync(await client.GetAsync($"/albums/{albumId}/tracks/ids", Ct));
            Assert.Equal([second, first], ids.GetProperty("trackIds").EnumerateArray().Select(e => e.GetGuid()).ToList());

            var updated = await client.PutAsJsonAsync($"/albums/{albumId}", new UpdateAlbumRequest("Typed", null, 2024, AlbumType.Single), Ct);
            Assert.Equal("Single", (await ReadJsonAsync(updated)).GetProperty("type").GetString());
        }

        [Fact]
        public async Task PostAlbum_WithTrackOfAnotherUser_ReturnsForbidden()
        {
            var userId = await fixture.SeedUserAsync();
            var stranger = await fixture.SeedUserAsync();
            var theirs = await SeedTrackAsync(stranger, "Theirs");
            var client = fixture.CreateAuthenticatedClient(userId);
            MockStorage();
            var upload = await ReadJsonAsync(await client.PostAsJsonAsync("/albums/upload-picture", new PictureUploadRequest("webp", "image/webp"), Ct));

            var response = await client.PostAsJsonAsync(
                "/albums", new CreateAlbumRequest("Stolen", null, null, upload.GetProperty("intentId").GetGuid(), TrackIds: [theirs]), Ct);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
