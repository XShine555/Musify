using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Musify.Api.Tests.TestSupport;
using Musify.Application.Tracks.Responses;
using Musify.Application.Users.Responses;
using Musify.Domain.Entities;
using Musify.Infrastructure.Persistence;
using Xunit;

namespace Musify.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class UserEndpointsTests(ApiTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task SeedListenAsync(long userId, string trackTitle, DateTime listenedAt)
    {
        using var scope = fixture.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<Database>();
        var owner = await database.Users.SingleAsync(u => u.Id == userId);
        var track = new Track
        {
            Title = trackTitle,
            NormalizedTitle = trackTitle.ToUpperInvariant(),
            OwnerUserId = owner.Id,
            Owner = owner
        };
        database.Tracks.Add(track);
        database.ListeningHistories.Add(new ListeningHistory
        {
            UserId = userId,
            TrackId = track.Id,
            ListenedAt = listenedAt,
            IsCounted = true
        });
        await database.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PostUsers_IsNotExposed()
    {
        var client = fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/users", new { id = 1, name = "Intruder" }, TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsIt()
    {
        var client = fixture.CreateAnonymousClient();
        var userId = Random.Shared.NextInt64(1, long.MaxValue);
        await fixture.SeedUserAsync(userId, "Findable User");

        var response = await client.GetAsync($"/users/{userId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserApplicationResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Findable User", body?.Name);
    }

    [Fact]
    public async Task GetUserById_MissingUser_ReturnsNotFound()
    {
        var client = fixture.CreateAnonymousClient();

        var response = await client.GetAsync($"/users/{Random.Shared.NextInt64(1, long.MaxValue)}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLastTrackListenedByUserId_HasHistory_ReturnsMostRecentTrack()
    {
        var client = fixture.CreateAnonymousClient();
        var userId = Random.Shared.NextInt64(1, long.MaxValue);
        await fixture.SeedUserAsync(userId);
        await SeedListenAsync(userId, "Older listen", DateTime.UtcNow.AddMinutes(-10));
        await SeedListenAsync(userId, "Newer listen", DateTime.UtcNow);

        var response = await client.GetAsync($"/users/{userId}/last-listened-track", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TrackApplicationResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal("Newer listen", body?.Title);
    }

    [Fact]
    public async Task GetLastTrackListenedByUserId_NoHistory_ReturnsNoContent()
    {
        var client = fixture.CreateAnonymousClient();
        var userId = Random.Shared.NextInt64(1, long.MaxValue);
        await fixture.SeedUserAsync(userId);

        var response = await client.GetAsync($"/users/{userId}/last-listened-track", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageNumber=0")]
    public async Task GetUsers_InvalidPaging_ReturnsBadRequest(string query)
    {
        var client = fixture.CreateAnonymousClient();

        var response = await client.GetAsync($"/users?{query}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetListeningStats_AnotherUser_ReturnsForbidden()
    {
        var userId = await fixture.SeedUserAsync();
        var other = await fixture.SeedUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/users/{other}/listening-stats", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetListeningStats_Anonymous_ReturnsUnauthorized()
    {
        var response = await fixture.CreateAnonymousClient().GetAsync("/users/1/listening-stats", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
