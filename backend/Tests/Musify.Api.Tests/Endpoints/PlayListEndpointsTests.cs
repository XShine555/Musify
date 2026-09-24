using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Musify.Api.DataTransferObjects.PlayLists;
using Musify.Api.Tests.TestSupport;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class PlayListEndpointsTests(ApiTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<PlayListApplicationResponse> CreatePlayListAsync(long ownerId, PlayListVisibility visibility, string? name = null)
    {
        var response = await fixture.CreateAuthenticatedClient(ownerId).PostAsJsonAsync(
            "/playlists",
            new CreatePlayListRequest(name ?? $"pl-{Guid.NewGuid():N}"[..20], null, null, visibility),
            Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlayListApplicationResponse>(JsonOptions, Ct))!;
    }

    [Fact]
    public async Task GetPlayListById_PrivatePlayList_IsOnlyVisibleToItsOwner()
    {
        var owner = await fixture.SeedUserAsync();
        var stranger = await fixture.SeedUserAsync();
        var playList = await CreatePlayListAsync(owner, PlayListVisibility.Private);

        var asOwner = await fixture.CreateAuthenticatedClient(owner).GetAsync($"/playlists/{playList.Id}", Ct);
        var asStranger = await fixture.CreateAuthenticatedClient(stranger).GetAsync($"/playlists/{playList.Id}", Ct);
        var asAnonymous = await fixture.CreateAnonymousClient().GetAsync($"/playlists/{playList.Id}", Ct);

        Assert.Equal(HttpStatusCode.OK, asOwner.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, asStranger.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, asAnonymous.StatusCode);
    }

    [Fact]
    public async Task GetPlayListById_PublicPlayList_IsVisibleToAnonymousUsers()
    {
        var owner = await fixture.SeedUserAsync();
        var playList = await CreatePlayListAsync(owner, PlayListVisibility.Public);

        var response = await fixture.CreateAnonymousClient().GetAsync($"/playlists/{playList.Id}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayListTracks_PrivatePlayList_IsHiddenFromStrangers()
    {
        var owner = await fixture.SeedUserAsync();
        var stranger = await fixture.SeedUserAsync();
        var playList = await CreatePlayListAsync(owner, PlayListVisibility.Private);

        var asOwner = await fixture.CreateAuthenticatedClient(owner).GetAsync($"/playlists/{playList.Id}/tracks", Ct);
        var asStranger = await fixture.CreateAuthenticatedClient(stranger).GetAsync($"/playlists/{playList.Id}/tracks", Ct);

        Assert.Equal(HttpStatusCode.OK, asOwner.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, asStranger.StatusCode);
    }

    [Fact]
    public async Task GetPlayListsByUserId_StrangersOnlySeePublicPlayLists()
    {
        var owner = await fixture.SeedUserAsync();
        var stranger = await fixture.SeedUserAsync();
        var publicPlayList = await CreatePlayListAsync(owner, PlayListVisibility.Public);
        var privatePlayList = await CreatePlayListAsync(owner, PlayListVisibility.Private);

        var asStranger = await ReadPlayListIdsAsync(fixture.CreateAuthenticatedClient(stranger), owner);
        var asOwner = await ReadPlayListIdsAsync(fixture.CreateAuthenticatedClient(owner), owner);

        Assert.Equal([publicPlayList.Id], asStranger);
        Assert.Contains(publicPlayList.Id, asOwner);
        Assert.Contains(privatePlayList.Id, asOwner);
    }

    [Fact]
    public async Task UpdatePlayList_NotTheOwner_ReturnsForbidden()
    {
        var owner = await fixture.SeedUserAsync();
        var stranger = await fixture.SeedUserAsync();
        var playList = await CreatePlayListAsync(owner, PlayListVisibility.Public);

        var response = await fixture.CreateAuthenticatedClient(stranger).PutAsJsonAsync(
            $"/playlists/{playList.Id}", new UpdatePlayListRequest("Hijacked", null, null), Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeletePlayList_NotTheOwner_ReturnsForbidden()
    {
        var owner = await fixture.SeedUserAsync();
        var stranger = await fixture.SeedUserAsync();
        var playList = await CreatePlayListAsync(owner, PlayListVisibility.Public);

        var response = await fixture.CreateAuthenticatedClient(stranger).DeleteAsync($"/playlists/{playList.Id}", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlayList_Owner_RenamesIt()
    {
        var owner = await fixture.SeedUserAsync();
        var playList = await CreatePlayListAsync(owner, PlayListVisibility.Private);

        var response = await fixture.CreateAuthenticatedClient(owner).PutAsJsonAsync(
            $"/playlists/{playList.Id}", new UpdatePlayListRequest("Renamed", null, null), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PlayListApplicationResponse>(JsonOptions, Ct);
        Assert.Equal("Renamed", body!.Name);
    }

    [Fact]
    public async Task GetPlayListById_NotAGuid_ReturnsNotFound()
    {
        var response = await fixture.CreateAnonymousClient().GetAsync("/playlists/not-a-guid", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlayList_NameTooLong_ReturnsValidationProblem()
    {
        var owner = await fixture.SeedUserAsync();

        var response = await fixture.CreateAuthenticatedClient(owner).PostAsJsonAsync(
            "/playlists", new CreatePlayListRequest(new string('a', 51), null, null), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<IReadOnlyList<Guid>> ReadPlayListIdsAsync(HttpClient client, long userId)
    {
        var response = await client.GetAsync($"/playlists/users/{userId}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PaginatedResponse<PlayListApplicationResponse>>(JsonOptions, Ct);
        return page!.Items.Select(playList => playList.Id).ToList();
    }
}
