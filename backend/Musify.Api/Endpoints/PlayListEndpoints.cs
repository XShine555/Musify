using Mediator;
using Musify.Application.PlayLists;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Tracks.Responses;
using Musify.Application.Shared;
using Musify.Application.Contracts;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.PlayLists;
using Musify.Api.Extensions;
using Musify.Api.Filters;

namespace Musify.Api.Endpoints;

public static class PlayListEndpoints
{
    public static IEndpointRouteBuilder MapPlayListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlists")
            .WithTags("PlayLists");

        group.MapGet("/", GetPlayLists)
            .WithName("GetPlayLists")
            .WithSummary("Get Paginated PlayLists.")
            .Produces<PaginatedResponse<PlayListApplicationResponse>>();

        group.MapGet("/{id}", GetPlayListById)
            .WithName("GetPlayListById")
            .WithSummary("Get A PlayList By Id.")
            .Produces<PlayListApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/users/{userId}", GetPlayListsByUserId)
            .WithName("GetPlayListsByUserId")
            .WithSummary("Get Paginated PlayLists For A User.")
            .Produces<PaginatedResponse<PlayListApplicationResponse>>();

        group.MapGet("/{playlistId}/tracks", GetPlayListTracks)
            .WithName("GetPlayListTracks")
            .WithSummary("Get Paginated Tracks Of A PlayList.")
            .Produces<PaginatedResponse<TrackApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/cover", GetPlayListCover)
            .WithName("GetPlayListCover")
            .WithSummary("Get A PlayList Cover Image.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreatePlayList)
            .WithName("CreatePlayList")
            .WithSummary("Create A New PlayList.")
            .AddEndpointFilter<ValidationFilter<CreatePlayListRequest>>()
            .RequireAuthorization()
            .Produces<PlayListApplicationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{playlistId}", UpdatePlayList)
            .WithName("UpdatePlayList")
            .WithSummary("Update An Existing PlayList.")
            .AddEndpointFilter<ValidationFilter<UpdatePlayListRequest>>()
            .RequireAuthorization()
            .Produces<PlayListApplicationResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{playlistId}", DeletePlayList)
            .WithName("DeletePlayList")
            .WithSummary("Delete A PlayList.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/upload-picture", RequestPlayListPictureUpload)
            .WithName("RequestPlayListPictureUpload")
            .WithSummary("Request A Pre-Signed URL To Upload A PlayList Picture.")
            .AddEndpointFilter<ValidationFilter<RequestPlayListPictureUploadRequest>>()
            .RequireAuthorization()
            .Produces<PlayListPictureUploadResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{playlistId}/tracks/{trackId}", AddTrackToPlayList)
            .WithName("AddTrackToPlayList")
            .WithSummary("Add A Track To A PlayList.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{playlistId}/tracks/{trackId}", RemoveTrackFromPlayList)
            .WithName("RemoveTrackFromPlayList")
            .WithSummary("Remove A Track From A PlayList.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

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

    private static async Task<IResult> GetPlayListCover(
        IMediator mediator,
        IStorageService storageService,
        HttpResponse response,
        Guid id,
        CancellationToken cancellationToken,
        string size = "medium")
    {
        var result = await mediator.Send(new GetPlayListCoverQuery(id, size), cancellationToken);
        if (result.IsError)
            return Results.NotFound();

        var location = result.Value;
        var stream = await storageService.GetFileAsync(location.Bucket, location.Key, cancellationToken);
        response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Results.Stream(stream, location.ContentType);
    }

    private static async Task<IResult> CreatePlayList(
        IMediator mediator,
        CurrentUser currentUser,
        CreatePlayListRequest request,
        CancellationToken cancellationToken)
    {
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? "No description was provided."
            : request.Description;

        var result = await mediator.Send(
            new CreatePlayListCommand(currentUser.RequiredId, request.Name, description, request.PictureIntentId),
            cancellationToken);

        return result.ToCreatedResult(playList => $"/playlists/{playList.Id}");
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
