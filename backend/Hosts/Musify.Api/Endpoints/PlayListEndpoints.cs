using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects;
using Musify.Api.DataTransferObjects.PlayLists;
using Musify.Api.Extensions;
using Musify.Api.Filters;
using Musify.Application.Contracts;
using Musify.Application.Pictures;
using Musify.Application.Pictures.Responses;
using Musify.Application.PlayLists;
using Musify.Application.PlayLists.Responses;
using Musify.Api.Models;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;

namespace Musify.Api.Endpoints;

public static class PlayListEndpoints
{
    public static IEndpointRouteBuilder MapPlayListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlists")
            .WithTags("PlayLists");

        group.MapGet("/{id}", GetPlayListById)
            .WithName("GetPlayListById")
            .WithSummary("Get A PlayList By Id.")
            .Produces<PlayListApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/users/{userId}", GetPlayListsByUserId)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetPlayListsByUserId")
            .WithSummary("Get Paginated PlayLists For A User. Private PlayLists Are Only Returned To Their Owner.")
            .Produces<PaginatedResponse<PlayListApplicationResponse>>();

        group.MapGet("/{playlistId}/tracks", GetPlayListTracks)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
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
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{playlistId}", DeletePlayList)
            .WithName("DeletePlayList")
            .WithSummary("Delete A PlayList.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/upload-picture", RequestPlayListPictureUpload)
            .WithName("RequestPlayListPictureUpload")
            .WithSummary("Request A Pre-Signed URL To Upload A PlayList Picture.")
            .AddEndpointFilter<ValidationFilter<RequestPlayListPictureUploadRequest>>()
            .RequireAuthorization()
            .Produces<PictureUploadResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/tracks", AddPlayListTracks)
            .WithName("AddPlayListTracks")
            .WithSummary("Add Tracks To A PlayList, Skipping The Ones It Already Has.")
            .AddEndpointFilter<ValidationFilter<AddTracksRequest>>()
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{playlistId}/tracks/{trackId}", RemoveTrackFromPlayList)
            .WithName("RemoveTrackFromPlayList")
            .WithSummary("Remove A Track From A PlayList.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetPlayListById(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPlayListByIdQuery(id, currentUser.Id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPlayListsByUserId(
        IMediator mediator,
        CurrentUser currentUser,
        long userId,
        CancellationToken cancellationToken,
        string? name,
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetPlayListsByUserIdQuery(userId, name, page.PageNumber, page.PageSize, currentUser.Id), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPlayListTracks(
        IMediator mediator,
        CurrentUser currentUser,
        Guid playlistId,
        CancellationToken cancellationToken,
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetPlayListTracksQuery(playlistId, page.PageNumber, page.PageSize, currentUser.Id), cancellationToken);
        return result.ToHttpResult();
    }

    private static Task<IResult> GetPlayListCover(
        IMediator mediator,
        IStorageService storageService,
        HttpResponse response,
        Guid id,
        CancellationToken cancellationToken,
        string size = "medium")
    {
        return CoverEndpoint.Stream(mediator, storageService, response, CoverOwner.PlayList, id, size, cancellationToken);
    }

    private static async Task<IResult> CreatePlayList(
        IMediator mediator,
        CurrentUser currentUser,
        CreatePlayListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreatePlayListCommand(currentUser.RequiredId, request.Name, request.Description, request.PictureIntentId, request.Visibility),
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
            new UpdatePlayListCommand(currentUser.RequiredId, playlistId, request.NewName, request.NewDescription, request.NewPictureIntentId, request.NewVisibility),
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

    private static async Task<IResult> AddPlayListTracks(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        AddTracksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddTracksToPlayListCommand(currentUser.RequiredId, id, request.TrackIds),
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
