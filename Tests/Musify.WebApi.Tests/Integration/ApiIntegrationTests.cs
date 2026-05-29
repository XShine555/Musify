using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Users.Responses;
using NSubstitute;
using WebApi.DataTransferObjects.PlayLists;
using WebApi.DataTransferObjects.Tracks;
using WebApi.DataTransferObjects.Users;
using Xunit;

namespace Musify.WebApi.Tests.Integration;

public sealed class ApiIntegrationTests(MusifyApiFactory factory)
    : IClassFixture<MusifyApiFactory>
{
    private sealed record ValidationProblemResponse(Dictionary<string, string[]> Errors);

    private sealed record TrackUploadUrlsLikeResponse(string PictureUploadUrl, string AudioUploadUrl);

    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Guid> CreateUserAsync(string name = "IntegrationUser")
    {
        var id = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync("/users/", new CreateUserRequest(id, name, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return id;
    }

    [Fact]
    public async Task Create_then_get_user_roundtrips_through_the_real_pipeline()
    {
        var id = Guid.NewGuid();

        var create = await _client.PostAsJsonAsync("/users/", new CreateUserRequest(id, "Alice", "Al", "Ice"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var get = await _client.GetAsync($"/users/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        UserApplicationResponse user = await get.Content.ReadFromJsonAsync<UserApplicationResponse>()
            ?? throw new InvalidOperationException("The response body was null.");
        user.Id.Should().Be(id);
        user.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task Create_user_with_invalid_body_returns_400_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/users/", new CreateUserRequest(Guid.Empty, "", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationProblemResponse problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>()
            ?? throw new InvalidOperationException("The response body was null.");
        problem.Errors.Should().ContainKey(nameof(CreateUserRequest.Name));
    }

    [Fact]
    public async Task Create_duplicate_user_returns_409_conflict()
    {
        var id = await CreateUserAsync();

        var duplicate = await _client.PostAsJsonAsync("/users/", new CreateUserRequest(id, "Other", null, null));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_unknown_user_returns_404()
    {
        var response = await _client.GetAsync($"/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_playlist_without_picture_returns_201_and_persists()
    {
        var userId = await CreateUserAsync();

        var response = await _client.PostAsJsonAsync(
            $"/playLists/users/{userId}",
            new CreatePlayListRequest("My Mix", "A description", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        PlayListApplicationResponse playList = await response.Content.ReadFromJsonAsync<PlayListApplicationResponse>()
            ?? throw new InvalidOperationException("The response body was null.");
        playList.Name.Should().Be("My Mix");

        var get = await _client.GetAsync($"/playLists/{playList.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_playlist_for_unknown_user_returns_404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/playLists/users/{Guid.NewGuid()}",
            new CreatePlayListRequest("Mix", "desc", null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_playlist_with_invalid_body_returns_400()
    {
        var userId = await CreateUserAsync();

        var response = await _client.PostAsJsonAsync(
            $"/playLists/users/{userId}",
            new CreatePlayListRequest("", "", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_unknown_track_returns_404()
    {
        var response = await _client.GetAsync($"/tracks/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_track_upload_urls_issues_presigned_urls()
    {
        var userId = await CreateUserAsync();
        factory.Storage
            .GetUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://signed.example/put");

        var response = await _client.PostAsJsonAsync(
            $"/tracks/upload-urls/users/{userId}",
            new RequestTrackUploadUrlsRequest("png", "image/png", "mp3", "audio/mpeg"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TrackUploadUrlsLikeResponse body = await response.Content.ReadFromJsonAsync<TrackUploadUrlsLikeResponse>()
            ?? throw new InvalidOperationException("The response body was null.");
        body.PictureUploadUrl.Should().Be("https://signed.example/put");
        body.AudioUploadUrl.Should().Be("https://signed.example/put");
    }
}
