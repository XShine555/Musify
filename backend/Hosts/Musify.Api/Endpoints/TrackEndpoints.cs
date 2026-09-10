using Mediator;
using Musify.Application.Tracks;
using Musify.Application.Tracks.Responses;
using Musify.Application.Shared;
using Musify.Application.Contracts;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.Extensions;
using Musify.Api.Filters;

namespace Musify.Api.Endpoints;

public static class TrackEndpoints
{
    public static IEndpointRouteBuilder MapTrackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tracks")
            .WithTags("Tracks");

        group.MapGet("/", GetTracks)
            .WithName("GetTracks")
            .WithSummary("Get Paginated Tracks, Combined With Live YouTube Music Results When A Name Filter Is Given.")
            .Produces<TracksSearchResponse>();

        group.MapGet("/{id}", GetTrackById)
            .WithName("GetTrackById")
            .WithSummary("Get A Track By Id.")
            .Produces<TrackApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/cover", GetTrackCover)
            .WithName("GetTrackCover")
            .WithSummary("Get A Track Cover Image.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/stream", GetTrackStream)
            .WithName("GetTrackStream")
            .WithSummary("Get A Streaming Manifest URL And Ticket For A Track.")
            .RequireAuthorization()
            .Produces<TrackStreamResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/users/{userId}", GetTracksByUserId)
            .WithName("GetTracksByUserId")
            .WithSummary("Get Paginated Tracks For A User.")
            .Produces<PaginatedResponse<TrackApplicationResponse>>();

        group.MapPost("/", CreateTrack)
            .WithName("CreateTrack")
            .WithSummary("Create A New Track.")
            .AddEndpointFilter<ValidationFilter<CreateTrackRequest>>()
            .RequireAuthorization()
            .Produces<TrackApplicationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{trackId}", DeleteTrack)
            .WithName("DeleteTrack")
            .WithSummary("Delete A Track.")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/upload-urls", RequestTrackUploadUrls)
            .WithName("RequestTrackUploadUrls")
            .WithSummary("Request Pre-Signed URLs To Upload Track Picture And Audio.")
            .AddEndpointFilter<ValidationFilter<RequestTrackUploadUrlsRequest>>()
            .RequireAuthorization()
            .Produces<TrackUploadUrlsResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetTracks(
        IMediator mediator,
        CancellationToken cancellationToken,
        string? name,
        int pageNumber = 1,
        int pageSize = 10,
        string? youtubeContinuationToken = null)
    {
        var result = await mediator.Send(new GetTracksQuery(name, pageNumber, pageSize, youtubeContinuationToken), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTrackById(
        IMediator mediator,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTrackByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTrackCover(
        IMediator mediator,
        IStorageService storageService,
        HttpResponse response,
        Guid id,
        CancellationToken cancellationToken,
        string size = "medium")
    {
        var result = await mediator.Send(new GetTrackCoverQuery(id, size), cancellationToken);
        if (result.IsError)
            return Results.NotFound();

        var location = result.Value;
        var stream = await storageService.GetFileAsync(location.Bucket, location.Key, cancellationToken);
        response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Results.Stream(stream, location.ContentType);
    }

    private static async Task<IResult> GetTrackStream(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTrackStreamQuery(id, currentUser.RequiredId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTracksByUserId(
        IMediator mediator,
        long userId,
        CancellationToken cancellationToken,
        string? name,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetTracksByUserIdQuery(userId, name, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateTrack(
        IMediator mediator,
        CurrentUser currentUser,
        CreateTrackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTrackCommand(currentUser.RequiredId, request.Title, request.PictureIntentId, request.AudioIntentId),
            cancellationToken);

        return result.ToCreatedResult(track => $"/tracks/{track.Id}");
    }

    private static async Task<IResult> DeleteTrack(
        IMediator mediator,
        CurrentUser currentUser,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTrackCommand(currentUser.RequiredId, trackId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RequestTrackUploadUrls(
        IMediator mediator,
        CurrentUser currentUser,
        RequestTrackUploadUrlsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RequestTrackUploadUrlsCommand(
                currentUser.RequiredId,
                request.PictureFileType,
                request.PictureContentType,
                request.AudioFileType,
                request.AudioContentType,
                request.ExpectedPictureSizeBytes,
                request.ExpectedAudioSizeBytes),
            cancellationToken);

        return result.ToHttpResult();
    }
}
