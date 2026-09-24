using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects;
using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.Extensions;
using Musify.Api.Filters;
using Musify.Api.Models;
using Musify.Application.Albums;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Pictures;
using Musify.Application.Pictures.Responses;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;

namespace Musify.Api.Endpoints;

public static class AlbumEndpoints
{
    private const int MaxRecentAlbumsLimit = 50;

    public static IEndpointRouteBuilder MapAlbumEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/albums")
            .WithTags("Albums");

        group.MapGet("/", GetAlbums)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetAlbums")
            .WithSummary("Get paginated albums, optionally filtered by title")
            .Produces<PaginatedResponse<AlbumApplicationResponse>>();

        group.MapGet("/{id:guid}", GetAlbumById)
            .WithName("GetAlbumById")
            .WithSummary("Get an album by id")
            .Produces<AlbumApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/users/{userId:long}", GetAlbumsByUserId)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetAlbumsByUserId")
            .WithSummary("Get paginated albums for a user")
            .Produces<PaginatedResponse<AlbumApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/recent", GetRecentlyListenedAlbums)
            .WithName("GetRecentlyListenedAlbums")
            .WithSummary("Get the albums the current user listened to most recently")
            .RequireAuthorization()
            .Produces<IReadOnlyList<AlbumApplicationResponse>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}/tracks", GetAlbumTracks)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetAlbumTracks")
            .WithSummary("Get the tracks of an album ordered by track number")
            .Produces<PaginatedResponse<TrackApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/cover", GetAlbumCover)
            .WithName("GetAlbumCover")
            .WithSummary("Get an album cover image")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/upload-picture", RequestAlbumPictureUpload)
            .WithName("RequestAlbumPictureUpload")
            .WithSummary("Request a pre-signed URL to upload an album picture")
            .AddEndpointFilter<ValidationFilter<PictureUploadRequest>>()
            .RequireAuthorization()
            .Produces<PictureUploadResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAlbum)
            .WithName("CreateAlbum")
            .WithSummary("Create a new album")
            .AddEndpointFilter<ValidationFilter<CreateAlbumRequest>>()
            .RequireAuthorization()
            .Produces<AlbumApplicationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAlbum)
            .WithName("UpdateAlbum")
            .WithSummary("Update an album")
            .AddEndpointFilter<ValidationFilter<UpdateAlbumRequest>>()
            .RequireAuthorization()
            .Produces<AlbumApplicationResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAlbum)
            .WithName("DeleteAlbum")
            .WithSummary("Delete an album")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/tracks", AddAlbumTracks)
            .WithName("AddAlbumTracks")
            .WithSummary("Add your uploaded tracks to an album, skipping the ones it already has")
            .AddEndpointFilter<ValidationFilter<AddTracksRequest>>()
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/tracks/{trackId:guid}", RemoveTrackFromAlbum)
            .WithName("RemoveTrackFromAlbum")
            .WithSummary("Remove a track from an album")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetAlbums(
        IMediator mediator,
        CancellationToken cancellationToken,
        string? title,
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetAlbumsQuery(title, page.PageNumber, page.PageSize), cancellationToken);
        return Results.Ok(result);
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
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetAlbumsByUserIdQuery(userId, page.PageNumber, page.PageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetRecentlyListenedAlbums(
        IMediator mediator,
        CurrentUser currentUser,
        CancellationToken cancellationToken,
        int limit = 12)
    {
        if (limit is < 1 or > MaxRecentAlbumsLimit)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(limit)] = [$"'{nameof(limit)}' must be between 1 and {MaxRecentAlbumsLimit}."]
            });
        }

        var result = await mediator.Send(new GetRecentlyListenedAlbumsQuery(currentUser.RequiredId, limit), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAlbumTracks(
        IMediator mediator,
        Guid id,
        CancellationToken cancellationToken,
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetAlbumTracksQuery(id, page.PageNumber, page.PageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static Task<IResult> GetAlbumCover(
        IMediator mediator,
        IStorageService storageService,
        HttpResponse response,
        Guid id,
        CancellationToken cancellationToken,
        string size = "medium")
    {
        return CoverEndpoint.Stream(mediator, storageService, response, CoverOwner.Album, id, size, cancellationToken);
    }

    private static async Task<IResult> RequestAlbumPictureUpload(
        IMediator mediator,
        CurrentUser currentUser,
        PictureUploadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RequestAlbumPictureUploadCommand(currentUser.RequiredId, request.FileType, request.ContentType, request.ExpectedSizeBytes),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        CreateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateAlbumCommand(currentUser.RequiredId, request.Title, request.Description, request.ReleaseYear, request.PictureIntentId),
            cancellationToken);

        return result.ToCreatedResult(album => $"/albums/{album.Id}");
    }

    private static async Task<IResult> UpdateAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        UpdateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateAlbumCommand(currentUser.RequiredId, id, request.Title, request.Description, request.ReleaseYear, request.PictureIntentId),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteAlbumCommand(currentUser.RequiredId, id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AddAlbumTracks(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        AddTracksRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddTracksToAlbumCommand(currentUser.RequiredId, id, request.TrackIds),
            cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveTrackFromAlbum(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RemoveTrackFromAlbumCommand(currentUser.RequiredId, id, trackId),
            cancellationToken);

        return result.ToHttpResult();
    }
}
