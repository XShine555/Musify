using Mediator;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Queries;
using WebApi.DataTransferObjects.PlayLists;
using WebApi.Extensions;

namespace WebApi.Endpoints;

public static class PlayListEndpoints
{
    public static IEndpointRouteBuilder MapPlayListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/playlists")
            .WithTags("PlayLists");

        group.MapGet("/", GetPlayLists)
            .WithName("GetPlayLists")
            .WithSummary("Get paginated playlists.");

        group.MapGet("/{id:guid}", GetPlayListById)
            .WithName("GetPlayListById")
            .WithSummary("Get a playlist by ID.");

        group.MapGet("/users/{userId:guid}", GetPlayListsByUserId)
            .WithName("GetPlayListsByUserId")
            .WithSummary("Get paginated playlists for a user.");

        group.MapPost("/users/{userId:guid}", CreatePlayList)
            .WithName("CreatePlayList")
            .WithSummary("Create a new playlist.");

        group.MapPut("/{playlistId:guid}/users/{userId:guid}", UpdatePlayList)
            .WithName("UpdatePlayList")
            .WithSummary("Update an existing playlist.");

        group.MapDelete("/{playlistId:guid}/users/{userId:guid}", DeletePlayList)
            .WithName("DeletePlayList")
            .WithSummary("Delete a playlist.");

        group.MapPost("/upload-picture/users/{userId:guid}", RequestPlayListPictureUpload)
            .WithName("RequestPlayListPictureUpload")
            .WithSummary("Request a pre-signed URL to upload a playlist picture.");

        return app;
    }

    private static async Task<IResult> GetPlayLists(
        IMediator mediator,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetPlayListsQuery(pageNumber, pageSize), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPlayListById(
        IMediator mediator,
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetPlayListByIdQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPlayListsByUserId(
        IMediator mediator,
        Guid userId,
        string? name,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetPlayListsByUserIdQuery(userId, name, pageNumber, pageSize), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreatePlayList(
        IMediator mediator,
        Guid userId,
        CreatePlayListRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreatePlayListCommand(userId, request.Name, request.Description, request.PictureIntentId),
            ct);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created($"/api/playlists/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> UpdatePlayList(
        IMediator mediator,
        Guid userId,
        Guid playlistId,
        UpdatePlayListRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdatePlayListCommand(userId, playlistId, request.NewName, request.NewDescription, request.NewPictureIntentId),
            ct);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeletePlayList(
        IMediator mediator,
        Guid userId,
        Guid playlistId,
        CancellationToken ct)
    {
        var result = await mediator.Send(new DeletePlayListCommand(userId, playlistId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RequestPlayListPictureUpload(
        IMediator mediator,
        Guid userId,
        RequestPlayListPictureUploadRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new RequestPlayListPictureUploadCommand(userId, request.FileType, request.ContentType, request.ExpectedSizeBytes),
            ct);

        return result.ToHttpResult();
    }
}
