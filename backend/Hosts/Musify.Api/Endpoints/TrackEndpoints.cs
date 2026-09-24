using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Api.Extensions;
using Musify.Api.Filters;
using Musify.Api.Models;
using Musify.Application.Contracts;
using Musify.Application.Pictures;
using Musify.Application.Shared;
using Musify.Application.Tracks;
using Musify.Application.Tracks.Responses;

namespace Musify.Api.Endpoints;

public static class TrackEndpoints
{
    public static IEndpointRouteBuilder MapTrackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tracks")
            .WithTags("Tracks");

        group.MapGet("/", GetTracks)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetTracks")
            .WithSummary("Get paginated tracks, optionally filtered by name and genre")
            .Produces<PaginatedResponse<TrackApplicationResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetTrackById)
            .WithName("GetTrackById")
            .WithSummary("Get a track by id")
            .Produces<TrackApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/cover", GetTrackCover)
            .WithName("GetTrackCover")
            .WithSummary("Get a track cover image")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/stream", GetTrackStream)
            .WithName("GetTrackStream")
            .WithSummary("Get a streaming manifest URL and ticket for a track. Works anonymously when the playback configuration allows it")
            .Produces<TrackStreamResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/listens/{listenId:guid}/progress", RecordListeningProgress)
            .AddEndpointFilter<ValidationFilter<RecordListeningProgressRequest>>()
            .WithName("RecordListeningProgress")
            .WithSummary("Report the total seconds actually played for a listen started by the stream endpoint")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/users/{userId:long}", GetTracksByUserId)
            .AddEndpointFilter<ValidationFilter<PageQuery>>()
            .WithName("GetTracksByUserId")
            .WithSummary("Get paginated tracks for a user")
            .Produces<PaginatedResponse<TrackApplicationResponse>>();

        group.MapPost("/", CreateTrack)
            .WithName("CreateTrack")
            .WithSummary("Create a new track")
            .AddEndpointFilter<ValidationFilter<CreateTrackRequest>>()
            .RequireAuthorization()
            .Produces<TrackApplicationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteTrack)
            .WithName("DeleteTrack")
            .WithSummary("Delete a track")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/upload-urls", RequestTrackUploadUrls)
            .WithName("RequestTrackUploadUrls")
            .WithSummary("Request pre-signed URLs to upload track picture and audio")
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
        string? name,
        GenreParameter? genre,
        [AsParameters] PageQuery page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTracksQuery(name, page.PageNumber, page.PageSize, genre?.Value), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTrackById(
        IMediator mediator,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTrackByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static Task<IResult> GetTrackCover(
        IMediator mediator,
        IStorageService storageService,
        HttpResponse response,
        Guid id,
        CancellationToken cancellationToken,
        string size = "medium")
    {
        return CoverEndpoint.Stream(mediator, storageService, response, CoverOwner.Track, id, size, cancellationToken);
    }

    private static async Task<IResult> GetTrackStream(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new StartListeningCommand(id, currentUser.Id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RecordListeningProgress(
        IMediator mediator,
        CurrentUser currentUser,
        Guid listenId,
        RecordListeningProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RecordListeningProgressCommand(currentUser.RequiredId, listenId, request.PlayedSeconds),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTracksByUserId(
        IMediator mediator,
        long userId,
        string? name,
        [AsParameters] PageQuery page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTracksByUserIdQuery(userId, name, page.PageNumber, page.PageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateTrack(
        IMediator mediator,
        CurrentUser currentUser,
        CreateTrackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTrackCommand(currentUser.RequiredId, request.Title, request.PictureIntentId, request.AudioIntentId, request.Tags, request.IsExplicit),
            cancellationToken);

        return result.ToCreatedResult(track => $"/tracks/{track.Id}");
    }

    private static async Task<IResult> DeleteTrack(
        IMediator mediator,
        CurrentUser currentUser,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTrackCommand(currentUser.RequiredId, id), cancellationToken);
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
