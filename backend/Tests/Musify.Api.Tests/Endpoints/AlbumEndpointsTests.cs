using System.Net;
using System.Net.Http.Json;
using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.Tests.TestSupport;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using NSubstitute;
using Xunit;

namespace Musify.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class AlbumEndpointsTests(ApiTestFixture fixture)
{
    private Task<long> CreateUserAsync() => fixture.SeedUserAsync();

    private async Task<Guid> CreatePictureIntentAsync(HttpClient client)
    {
        fixture.StorageService
            .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.musify.test/presigned-upload");
        fixture.StorageService
            .HeadObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ObjectMetaData("image/webp", 1024));

        var response = await client.PostAsJsonAsync(
            "/albums/upload-picture",
            new RequestAlbumPictureUploadRequest("webp", "image/webp"),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AlbumPictureUploadResponse>(TestContext.Current.CancellationToken);
        return body!.IntentId;
    }

    [Fact]
    public async Task PostAlbums_NoAuthHeader_ReturnsUnauthorized()
    {
        var response = await fixture.CreateAnonymousClient()
            .PostAsJsonAsync("/albums", new CreateAlbumRequest("Unauthorized Album", null, null, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAlbums_AuthenticatedExistingUser_ReturnsCreatedAlbum()
    {
        var userId = await CreateUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);
        var pictureIntentId = await CreatePictureIntentAsync(client);

        var response = await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("My Album", "desc", 2024, pictureIntentId), TestContext.Current.CancellationToken);

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

        var response = await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("", null, null, Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAlbums_MissingPictureIntent_ReturnsValidationProblem()
    {
        var userId = await CreateUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("No Picture", null, null, Guid.Empty), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAlbumById_AfterCreate_ReturnsIt()
    {
        var userId = await CreateUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);
        var pictureIntentId = await CreatePictureIntentAsync(client);
        var created = await (await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("Findable", null, null, pictureIntentId), TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<AlbumApplicationResponse>(TestContext.Current.CancellationToken);

        var response = await fixture.CreateAnonymousClient().GetAsync($"/albums/{created!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAlbum_NotTheOwner_ReturnsForbidden()
    {
        var owner = await CreateUserAsync();
        var stranger = await CreateUserAsync();
        var ownerClient = fixture.CreateAuthenticatedClient(owner);
        var pictureIntentId = await CreatePictureIntentAsync(ownerClient);
        var created = await (await ownerClient.PostAsJsonAsync("/albums", new CreateAlbumRequest("Owned", null, null, pictureIntentId), TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<AlbumApplicationResponse>(TestContext.Current.CancellationToken);

        var response = await fixture.CreateAuthenticatedClient(stranger).DeleteAsync($"/albums/{created!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAlbum_Owner_ReturnsNoContentAndAlbumIsGone()
    {
        var userId = await CreateUserAsync();
        var client = fixture.CreateAuthenticatedClient(userId);
        var pictureIntentId = await CreatePictureIntentAsync(client);
        var created = await (await client.PostAsJsonAsync("/albums", new CreateAlbumRequest("Deletable", null, null, pictureIntentId), TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<AlbumApplicationResponse>(TestContext.Current.CancellationToken);

        var deleteResponse = await client.DeleteAsync($"/albums/{created!.Id}", TestContext.Current.CancellationToken);
        var getResponse = await fixture.CreateAnonymousClient().GetAsync($"/albums/{created.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
