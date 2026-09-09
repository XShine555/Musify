using System.Net;
using System.Net.Http.Json;
using Musify.Api.DataTransferObjects.Users;
using Musify.Api.Tests.TestSupport;
using Musify.Application.Users.Responses;
using Xunit;

namespace Musify.Api.Tests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class UserEndpointsTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task PostUsers_NewUser_ReturnsCreatedWithLocationHeader()
    {
        var client = fixture.CreateAnonymousClient();
        var userId = Random.Shared.NextInt64(1, long.MaxValue);

        var response = await client.PostAsJsonAsync("/users", new CreateUserRequest(userId, "Jane Doe", "Jane", "Doe"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/users/{userId}", response.Headers.Location?.OriginalString);
        var body = await response.Content.ReadFromJsonAsync<UserApplicationResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("Jane Doe", body.Name);
    }

    [Fact]
    public async Task PostUsers_BlankName_ReturnsValidationProblem()
    {
        var client = fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/users", new CreateUserRequest(Random.Shared.NextInt64(1, long.MaxValue), "", null, null), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_DuplicateId_ReturnsConflict()
    {
        var client = fixture.CreateAnonymousClient();
        var userId = Random.Shared.NextInt64(1, long.MaxValue);
        await client.PostAsJsonAsync("/users", new CreateUserRequest(userId, "First", null, null), TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync("/users", new CreateUserRequest(userId, "Second", null, null), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsIt()
    {
        var client = fixture.CreateAnonymousClient();
        var userId = Random.Shared.NextInt64(1, long.MaxValue);
        await client.PostAsJsonAsync("/users", new CreateUserRequest(userId, "Findable User", null, null), TestContext.Current.CancellationToken);

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
}
