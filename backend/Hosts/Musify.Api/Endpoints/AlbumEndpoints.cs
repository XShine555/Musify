using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.Extensions;
using Musify.Api.Filters;
using Musify.Application.Albums;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Api.Models;
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
            .WithSummary("Get Paginated Albums, Optionally Filtered By Title.")
            .Produces<AlbumsSearchResponse>();

        group.MapGet("/{id}", GetAlbumById)
            .WithName("GetAlbumById")
            .WithSummary("Get An Album By Id.")
            .Produces<AlbumApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/users/{userId}", GetAlbumsByUserId)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetAlbumsByUserId")
            .WithSummary("Get Paginated Albums For A User.")
            .Produces<PaginatedResponse<AlbumApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/recent", GetRecentlyListenedAlbums)
            .WithName("GetRecentlyListenedAlbums")
            .WithSummary("Get The Albums The Current User Listened To Most Recently.")
            .RequireAuthorization()
            .Produces<IReadOnlyList<AlbumApplicationResponse>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{albumId}/tracks", GetAlbumTracks)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetAlbumTracks")
            .WithSummary("Get The Tracks Of An Album Ordered By Track Number.")
            .Produces<PaginatedResponse<TrackApplicationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/cover", GetAlbumCover)
            .WithName("GetAlbumCover")
            .WithSummary("Get An Album Cover Image.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/upload-picture", RequestAlbumPictureUpload)
            .WithName("RequestAlbumPictureUpload")
            .WithSummary("Request A Pre-Signed URL To Upload An Album Picture.")
            .AddEndpointFilter<ValidationFilter<RequestAlbumPictureUploadRequest>>()
            .RequireAuthorization()
            .Produces<AlbumPictureUploadResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
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

        group.MapPut("/{albumId}", UpdateAlbum)
            .WithName("UpdateAlbum")
            .WithSummary("Update An Album.")
            .AddEndpointFilter<ValidationFilter<UpdateAlbumRequest>>()
            .RequireAuthorization()
            .Produces<AlbumApplicationResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{albumId}", DeleteAlbum)
            .WithName("DeleteAlbum")
            .WithSummary("Delete An Album.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{albumId}/tracks/{trackId}", AddTrackToAlbum)
            .WithName("AddTrackToAlbum")
            .WithSummary("Add One Of Your Uploaded Tracks To An Album.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{albumId}/tracks/{trackId}", RemoveTrackFromAlbum)
            .WithName("RemoveTrackFromAlbum")
            .WithSummary("Remove A Track From An Album.")
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
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAlbumTracks(
        IMediator mediator,
        Guid albumId,
        CancellationToken cancellationToken,
        [AsParameters] PageQuery page)
    {
        var result = await mediator.Send(new GetAlbumTracksQuery(albumId, page.PageNumber, page.PageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAlbumCover(
        IMediator mediator,
        IStorageService storageService,
        HttpResponse response,
        Guid id,
        CancellationToken cancellationToken,
        string size = "medium")
    {
        var result = await mediator.Send(new GetAlbumCoverQuery(id, size), cancellationToken);
        if (result.IsError)
            return Results.NotFound();

        var location = result.Value;
        var stream = await storageService.GetFileAsync(location.Bucket, location.Key, cancellationToken);
        if (stream == null)
            return Results.NotFound();

        response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Results.Stream(stream, location.ContentType);
    }

    private static async Task<IResult> RequestAlbumPictureUpload(
        IMediator mediator,
        CurrentUser currentUser,
        RequestAlbumPictureUploadRequest request,
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
        Guid albumId,
        UpdateAlbumRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateAlbumCommand(currentUser.RequiredId, albumId, request.NewTitle, request.NewDescription, request.NewReleaseYear, request.NewPictureIntentId),
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
