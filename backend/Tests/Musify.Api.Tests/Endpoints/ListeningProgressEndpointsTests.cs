using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.DataTransferObjects.Users;
using Musify.Domain.Entities;
using Musify.Infrastructure.Persistence;
using Xunit;
using Musify.Api.Tests.TestSupport;

namespace Musify.Api.Tests.Endpoints
{
    [Collection(ApiCollection.Name)]
    public sealed class ListeningProgressEndpointsTests(ApiTestFixture fixture)
    {
        private async Task<long> CreateUserAsync()
        {
            var userId = Random.Shared.NextInt64(1, long.MaxValue);
            var response = await fixture.CreateAnonymousClient().PostAsJsonAsync(
                "/users", new CreateUserRequest(userId, $"user-{userId}", null, null), TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            return userId;
        }

        private async Task<Guid> SeedListenAsync(long userId, double durationSeconds)
        {
            using var scope = fixture.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            var owner = await database.Users.SingleAsync(u => u.Id == userId, TestContext.Current.CancellationToken);
            var track = new Track
            {
                Title = "Progress track",
                NormalizedTitle = "PROGRESS TRACK",
                OwnerUserId = owner.Id,
                Owner = owner,
                DurationSeconds = durationSeconds
            };
            var listen = new ListeningHistory
            {
                UserId = userId,
                TrackId = track.Id,
                ListenedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            database.Tracks.Add(track);
            database.ListeningHistories.Add(listen);
            await database.SaveChangesAsync(TestContext.Current.CancellationToken);
            return listen.Id;
        }

        private async Task<ListeningHistory> LoadListenAsync(Guid listenId)
        {
            using var scope = fixture.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<Database>();
            return await database.ListeningHistories.AsNoTracking()
                .SingleAsync(l => l.Id == listenId, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task PutProgress_Anonymous_ReturnsUnauthorized()
        {
            var response = await fixture.CreateAnonymousClient().PutAsJsonAsync(
                $"/tracks/listens/{Guid.NewGuid()}/progress", new RecordListeningProgressRequest(10), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PutProgress_OwnListen_StoresSecondsAndMarksCounted()
        {
            var userId = await CreateUserAsync();
            var listenId = await SeedListenAsync(userId, durationSeconds: 200);

            var response = await fixture.CreateAuthenticatedClient(userId).PutAsJsonAsync(
                $"/tracks/listens/{listenId}/progress", new RecordListeningProgressRequest(45), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var stored = await LoadListenAsync(listenId);
            Assert.Equal(45, stored.PlayedSeconds);
            Assert.True(stored.IsCounted);
        }

        [Fact]
        public async Task PutProgress_OtherUsersListen_ReturnsNotFound()
        {
            var ownerId = await CreateUserAsync();
            var otherId = await CreateUserAsync();
            var listenId = await SeedListenAsync(ownerId, durationSeconds: 200);

            var response = await fixture.CreateAuthenticatedClient(otherId).PutAsJsonAsync(
                $"/tracks/listens/{listenId}/progress", new RecordListeningProgressRequest(45), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null((await LoadListenAsync(listenId)).PlayedSeconds);
        }

        [Fact]
        public async Task PutProgress_NegativeSeconds_ReturnsBadRequest()
        {
            var userId = await CreateUserAsync();
            var listenId = await SeedListenAsync(userId, durationSeconds: 200);

            var response = await fixture.CreateAuthenticatedClient(userId).PutAsJsonAsync(
                $"/tracks/listens/{listenId}/progress", new RecordListeningProgressRequest(-5), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
