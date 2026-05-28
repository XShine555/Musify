using Mediator;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Queries;
using WebApi.DataTransferObjects.PlayLists;
using WebApi.Extensions;
using WebApi.Filters;

namespace WebApi.Endpoints;

public static class PlayListEndpoints
{
    public static IEndpointRouteBuilder MapPlayListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlists")
            .WithTags("PlayLists")
            .AddEndpointFilter<ValidationFilter>();

        group.MapGet("/", GetPlayLists)
            .WithName("GetPlayLists")
            .WithSummary("Get paginated playlists.");

        group.MapGet("/{id}", GetPlayListById)
            .WithName("GetPlayListById")
            .WithSummary("Get a playlist by ID.");

        group.MapGet("/users/{userId}", GetPlayListsByUserId)
            .WithName("GetPlayListsByUserId")
            .WithSummary("Get paginated playlists for a user.");

        group.MapPost("/users/{userId}", CreatePlayList)
            .WithName("CreatePlayList")
            .WithSummary("Create a new playlist.");

        group.MapPut("/{playlistId}/users/{userId}", UpdatePlayList)
            .WithName("UpdatePlayList")
            .WithSummary("Update an existing playlist.");

        group.MapDelete("/{playlistId}/users/{userId}", DeletePlayList)
            .WithName("DeletePlayList")
            .WithSummary("Delete a playlist.");

        group.MapPost("/upload-picture/users/{userId}", RequestPlayListPictureUpload)
            .WithName("RequestPlayListPictureUpload")
            .WithSummary("Request a pre-signed URL to upload a playlist picture.");

        return app;
    }

    private static async Task<IResult> GetPlayLists(
        IMediator mediator,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetPlayListsQuery(pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPlayListById(
        IMediator mediator,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPlayListByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPlayListsByUserId(
        IMediator mediator,
        Guid userId,
        CancellationToken cancellationToken,
        string? name = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetPlayListsByUserIdQuery(userId, name, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreatePlayList(
        IMediator mediator,
        Guid userId,
        CreatePlayListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreatePlayListCommand(userId, request.Name, request.Description, request.PictureIntentId),
            cancellationToken);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created($"/playlists/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> UpdatePlayList(
        IMediator mediator,
        Guid userId,
        Guid playlistId,
        UpdatePlayListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdatePlayListCommand(userId, playlistId, request.NewName, request.NewDescription, request.NewPictureIntentId),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeletePlayList(
        IMediator mediator,
        Guid userId,
        Guid playlistId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeletePlayListCommand(userId, playlistId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RequestPlayListPictureUpload(
        IMediator mediator,
        Guid userId,
        RequestPlayListPictureUploadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RequestPlayListPictureUploadCommand(userId, request.FileType, request.ContentType, request.ExpectedSizeBytes),
            cancellationToken);

        return result.ToHttpResult();
    }
}
