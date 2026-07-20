using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.Extensions;
using Musify.Api.Filters;
using Musify.Application.Albums;
using Musify.Application.Albums.Responses;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;

namespace Musify.Api.Endpoints;

public static class AlbumEndpoints
{
    public static IEndpointRouteBuilder MapAlbumEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/albums")
            .WithTags("Albums");

        group.MapGet("/", GetAlbums)
            .WithName("GetAlbums")
            .WithSummary("Get Paginated Albums.")
            .Produces<PaginatedResponse<AlbumApplicationResponse>>();

        group.MapGet("/{id}", GetAlbumById)
            .WithName("GetAlbumById")
            .WithSummary("Get An Album By Id.")
            .Produces<AlbumApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/users/{userId}", GetAlbumsByUserId)
            .WithName("GetAlbumsByUserId")
            .WithSummary("Get Paginated Albums For A User.")
            .Produces<PaginatedResponse<AlbumApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{albumId}/tracks", GetAlbumTracks)
            .WithName("GetAlbumTracks")
            .WithSummary("Get The Tracks Of An Album Ordered By Track Number.")
            .Produces<PaginatedResponse<TrackApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAlbum)
            .WithName("CreateAlbum")
            .WithSummary("Create A New Album.")
            .AddEndpointFilter<ValidationFilter<CreateAlbumRequest>>()
            .RequireAuthorization()
            .Produces<AlbumApplicationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/youtube/{albumId}", SaveYouTubeAlbum)
            .WithName("SaveYouTubeAlbum")
            .WithSummary("Save A YouTube Music Album To Your Library With All Of Its Tracks.")
            .RequireAuthorization()
            .Produces<AlbumApplicationResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{albumId}", UpdateAlbum)
            .WithName("UpdateAlbum")
            .WithSummary("Update An Album.")
            .AddEndpointFilter<ValidationFilter<UpdateAlbumRequest>>()
            .RequireAuthorization()
            .Produces<AlbumApplicationResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{albumId}", DeleteAlbum)
            .WithName("DeleteAlbum")
            .WithSummary("Delete An Album.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{albumId}/tracks/{trackId}", AddTrackToAlbum)
            .WithName("AddTrackToAlbum")
            .WithSummary("Add One Of Your Uploaded Tracks To An Album.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{albumId}/tracks/{trackId}", RemoveTrackFromAlbum)
            .WithName("RemoveTrackFromAlbum")
            .WithSummary("Remove A Track From An Album.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetAlbums(
        IMediator mediator,
        CancellationToken cancellationToken,
        string? title,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetAlbumsQuery(title, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAlbumById(
        IMediator mediator,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAlbumByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAlbumsByUserId(
        IMediator mediator,
        long userId,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetAlbumsByUserIdQuery(userId, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAlbumTracks(
        IMediator mediator,
        Guid albumId,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var result = await mediator.Send(new GetAlbumTracksQuery(albumId, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        CreateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateAlbumCommand(currentUser.RequiredId, request.Title, request.Description, request.ReleaseYear),
            cancellationToken);

        return result.ToCreatedResult(album => $"/albums/{album.Id}");
    }

    private static async Task<IResult> SaveYouTubeAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        string albumId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SaveYouTubeAlbumCommand(currentUser.RequiredId, albumId),
            cancellationToken);

        return result.ToCreatedResult(album => $"/albums/{album.Id}");
    }

    private static async Task<IResult> UpdateAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        Guid albumId,
        UpdateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateAlbumCommand(currentUser.RequiredId, albumId, request.NewTitle, request.NewDescription, request.NewReleaseYear),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        Guid albumId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAlbumCommand(currentUser.RequiredId, albumId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AddTrackToAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        Guid albumId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddTrackToAlbumCommand(currentUser.RequiredId, albumId, trackId),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveTrackFromAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        Guid albumId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RemoveTrackFromAlbumCommand(currentUser.RequiredId, albumId, trackId),
            cancellationToken);

        return result.ToHttpResult();
    }
}
