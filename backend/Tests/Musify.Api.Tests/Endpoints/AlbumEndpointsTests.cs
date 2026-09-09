using System.Net;
using System.Net.Http.Json;
using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.DataTransferObjects.Users;
using Musify.Api.Tests.TestSupport;
using Musify.Application.Albums.Responses;
using Xunit;

namespace Musify.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class AlbumEndpointsTests(ApiTestFixture fixture)
{
    private async Task<long> CreateUserAsync()
    {
        var userId = Random.Shared.NextInt64(1, long.MaxValue);
        var response = await fixture.CreateAnonymousClient()
            .PostAsJsonAsync("/users", new CreateUserRequest(userId, $"user-{userId}", null, null));
        response.EnsureSuccessStatusCode();
        return userId;
    }

    [Fact]
    public async Task PostAlbums_NoAuthHeader_ReturnsUnauthorized()
    {
        var response = await fixture.CreateAnonymousClient()
            .PostAsJsonAsync("/albums", new CreateAlbumRequest("Unauthorized Album", null, null), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAlbums_AuthenticatedExistingUser_ReturnsCreatedAlbum()
    {
        var userId = await CreateUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("My Album", "desc", 2024), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AlbumApplicationResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("My Album", body.Title);
        Assert.Equal(userId, body.OwnerUserId);
    }

    [Fact]
    public async Task PostAlbums_BlankTitle_ReturnsValidationProblem()
    {
        var userId = await CreateUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("", null, null), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAlbumById_AfterCreate_ReturnsIt()
    {
        var userId = await CreateUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);
        var created = await (await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("Findable", null, null), TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<AlbumApplicationResponse>(TestContext.Current.CancellationToken);

        var response = await fixture.CreateAnonymousClient().GetAsync($"/albums/{created!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAlbum_NotTheOwner_ReturnsUnauthorized()
    {
        var owner = await CreateUserAsync();
        var stranger = await CreateUserAsync();
        var created = await (await fixture.CreateAuthenticatedClient(owner).PostAsJsonAsync("/albums", new CreateAlbumRequest("Owned", null, null), TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<AlbumApplicationResponse>(TestContext.Current.CancellationToken);

        var response = await fixture.CreateAuthenticatedClient(stranger).DeleteAsync($"/albums/{created!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAlbum_Owner_ReturnsNoContentAndAlbumIsGone()
    {
        var userId = await CreateUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);
        var created = await (await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("Deletable", null, null), TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<AlbumApplicationResponse>(TestContext.Current.CancellationToken);

        var deleteResponse = await client.DeleteAsync($"/albums/{created!.Id}", TestContext.Current.CancellationToken);
        var getResponse = await fixture.CreateAnonymousClient().GetAsync($"/albums/{created.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
