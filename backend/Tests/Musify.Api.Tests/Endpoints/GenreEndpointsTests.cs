using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Musify.Api.Tests.TestSupport;
using Musify.Application.Genres.Responses;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Persistence;
using Xunit;

namespace Musify.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class GenreEndpointsTests(ApiTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<string> SeedTrackAsync(Genre genre)
    {
        using var scope = fixture.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<Database>();
        var owner = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = "genre-owner", NormalizedName = "GENRE-OWNER" };
        var title = $"Genre track {Guid.NewGuid():N}";
        var track = new Track
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            OwnerUserId = owner.Id,
            Owner = owner,
            Tags = [new TrackTag { TrackId = Guid.Empty, Tag = genre }]
        };
        foreach (var tag in track.Tags)
            tag.TrackId = track.Id;
        database.Users.Add(owner);
        database.Tracks.Add(track);
        database.UserHasTracks.Add(new UserHasTrack { UserId = owner.Id, TrackId = track.Id });
        await database.SaveChangesAsync(TestContext.Current.CancellationToken);
        return title;
    }

    [Fact]
    public async Task GetGenres_Anonymous_ListsGenresInUse()
    {
        await SeedTrackAsync(Genre.Funk);

        var response = await fixture.CreateAnonymousClient().GetAsync("/genres", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<GenreResponse>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Contains(body!, g => g.Genre == Genre.Funk && g.TrackCount >= 1);
    }

    [Fact]
    public async Task GetAvailableGenres_Anonymous_ListsEveryGenreWithIncompatibilities()
    {
        var response = await fixture.CreateAnonymousClient().GetAsync("/genres/available", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<AvailableGenreResponse>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(Enum.GetValues<Genre>().Length, body!.Count);
        Assert.Contains(Genre.Lofi, body.Single(g => g.Genre == Genre.Metal).IncompatibleWith);
    }

    [Theory]
    [InlineData("Nope")]
    [InlineData("999")]
    public async Task GetTracks_UnknownGenre_ReturnsBadRequest(string genre)
    {
        var response = await fixture.CreateAnonymousClient().GetAsync($"/tracks?genre={genre}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTracks_GenreQuery_IsCaseInsensitive()
    {
        var title = await SeedTrackAsync(Genre.Blues);

        var response = await fixture.CreateAnonymousClient().GetAsync("/tracks?genre=blues&pageSize=100", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TrackApplicationResponse>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Contains(body!.Items, item => item.Title == title);
    }

    [Fact]
    public async Task GetTracks_GenreQuery_FiltersByTag()
    {
        var title = await SeedTrackAsync(Genre.Ambient);
        await SeedTrackAsync(Genre.Techno);

        var response = await fixture.CreateAnonymousClient().GetAsync("/tracks?genre=Ambient&pageSize=100", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TrackApplicationResponse>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.Contains(body!.Items, item => item.Title == title);
        Assert.All(body.Items, item => Assert.Contains(Genre.Ambient, item.Tags));
    }
}
