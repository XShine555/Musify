using Mediator;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Queries;
using WebApi.Authentication;
using WebApi.DataTransferObjects.PlayLists;
using WebApi.Extensions;
using WebApi.Filters;

namespace WebApi.Endpoints;

public static class PlayListEndpoints
{
    public static IEndpointRouteBuilder MapPlayListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playLists")
            .WithTags("PlayLists");

        group.MapGet("/", GetPlayLists)
            .WithName("GetPlayLists")
            .WithSummary("Get Paginated PlayLists.");

        group.MapGet("/{id}", GetPlayListById)
            .WithName("GetPlayListById")
            .WithSummary("Get A PlayList By Id.");

        group.MapGet("/users/{userId}", GetPlayListsByUserId)
            .WithName("GetPlayListsByUserId")
            .WithSummary("Get Paginated PlayLists For A User.");

        group.MapGet("/{playlistId}/tracks", GetPlayListTracks)
            .WithName("GetPlayListTracks")
            .WithSummary("Get Paginated Tracks Of A PlayList.");

        group.MapPost("/", CreatePlayList)
            .WithName("CreatePlayList")
            .WithSummary("Create A New PlayList.")
            .AddEndpointFilter<ValidationFilter<CreatePlayListRequest>>()
            .RequireAuthorization();

        group.MapPut("/{playlistId}", UpdatePlayList)
            .WithName("UpdatePlayList")
            .WithSummary("Update An Existing PlayList.")
            .AddEndpointFilter<ValidationFilter<UpdatePlayListRequest>>()
            .RequireAuthorization();

        group.MapDelete("/{playlistId}", DeletePlayList)
            .WithName("DeletePlayList")
            .WithSummary("Delete A PlayList.")
            .RequireAuthorization();

        group.MapPost("/upload-picture", RequestPlayListPictureUpload)
            .WithName("RequestPlayListPictureUpload")
            .WithSummary("Request A Pre-Signed URL To Upload A PlayList Picture.")
            .AddEndpointFilter<ValidationFilter<RequestPlayListPictureUploadRequest>>()
            .RequireAuthorization();

        group.MapPost("/{playlistId}/tracks/{trackId}", AddTrackToPlayList)
            .WithName("AddTrackToPlayList")
            .WithSummary("Add A Track To A PlayList.")
            .RequireAuthorization();

        group.MapDelete("/{playlistId}/tracks/{trackId}", RemoveTrackFromPlayList)
            .WithName("RemoveTrackFromPlayList")
            .WithSummary("Remove A Track From A PlayList.")
            .RequireAuthorization();

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
        long userId,
        CancellationToken cancellationToken,
        string? name,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetPlayListsByUserIdQuery(userId, name, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPlayListTracks(
        IMediator mediator,
        Guid playlistId,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetPlayListTracksQuery(playlistId, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreatePlayList(
        IMediator mediator,
        CurrentUser currentUser,
        CreatePlayListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreatePlayListCommand(currentUser.RequiredId, request.Name, request.Description, request.PictureIntentId),
            cancellationToken);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created($"/playlists/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> UpdatePlayList(
        IMediator mediator,
        CurrentUser currentUser,
        Guid playlistId,
        UpdatePlayListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdatePlayListCommand(currentUser.RequiredId, playlistId, request.NewName, request.NewDescription, request.NewPictureIntentId),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeletePlayList(
        IMediator mediator,
        CurrentUser currentUser,
        Guid playlistId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeletePlayListCommand(currentUser.RequiredId, playlistId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RequestPlayListPictureUpload(
        IMediator mediator,
        CurrentUser currentUser,
        RequestPlayListPictureUploadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RequestPlayListPictureUploadCommand(currentUser.RequiredId, request.FileType, request.ContentType, request.ExpectedSizeBytes),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> AddTrackToPlayList(
        IMediator mediator,
        CurrentUser currentUser,
        Guid playlistId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddTrackToPlayListCommand(currentUser.RequiredId, playlistId, trackId),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveTrackFromPlayList(
        IMediator mediator,
        CurrentUser currentUser,
        Guid playlistId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RemoveTrackFromPlayListCommand(currentUser.RequiredId, playlistId, trackId),
            cancellationToken);

        return result.ToHttpResult();
    }
}
