using Mediator;
using Microsoft.AspNetCore.Mvc;
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
using Musify.Domain.ValueObjects;

namespace Musify.Api.Endpoints
{
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
                .WithSummary("Get paginated tracks for a user, optionally filtered by name, audio processing status and published state. Each track lists the albums it belongs to. Only the owner sees unpublished tracks")
                .Produces<PaginatedResponse<UserTrackApplicationResponse>>();

            group.MapGet("/users/{userId:long}/status-counts", GetTrackStatusCounts)
                .WithName("GetTrackStatusCounts")
                .WithSummary("Get how many of your tracks are in each audio processing status")
                .RequireAuthorization()
                .Produces<TrackStatusCountsResponse>()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            group.MapPost("/", CreateTrack)
                .WithName("CreateTrack")
                .WithSummary("Create a new track, unpublished, from upload intents. The picture is optional")
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

            group.MapDelete("/", DeleteTracks)
                .WithName("DeleteTracks")
                .WithSummary("Delete several of your tracks at once. Tracks that are missing or still processing are returned as failed")
                .AddEndpointFilter<ValidationFilter<DeleteTracksRequest>>()
                .RequireAuthorization()
                .Produces<DeleteTracksResponse>()
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            group.MapPut("/{id:guid}/published", SetTrackPublished)
                .WithName("SetTrackPublished")
                .WithSummary("Publish or unpublish one of your tracks. Only tracks with completed audio processing can be published")
                .RequireAuthorization()
                .Produces(StatusCodes.Status204NoContent)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);

            group.MapPut("/published", SetTracksPublished)
                .WithName("SetTracksPublished")
                .WithSummary("Publish or unpublish several of your tracks at once. Missing tracks and tracks that cannot be published yet are returned as skipped")
                .AddEndpointFilter<ValidationFilter<SetTracksPublishedRequest>>()
                .RequireAuthorization()
                .Produces<SetTracksPublishedResponse>()
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            group.MapPost("/upload-urls", RequestTrackUploadUrls)
                .WithName("RequestTrackUploadUrls")
                .WithSummary("Request pre-signed URLs to upload the audio of one or many tracks and an optional picture shared by all of them")
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
            CurrentUser currentUser,
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetTrackByIdQuery(id, currentUser.Id), cancellationToken);
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
            CurrentUser currentUser,
            long userId,
            string? name,
            [AsParameters] PageQuery page,
            CancellationToken cancellationToken,
            ProcessingStatus[]? status = null,
            bool? published = null)
        {
            var result = await mediator.Send(
                new GetTracksByUserIdQuery(userId, name, page.PageNumber, page.PageSize, status, currentUser.Id, published),
                cancellationToken);
            return result.ToHttpResult();
        }

        private static async Task<IResult> SetTrackPublished(
            IMediator mediator,
            CurrentUser currentUser,
            Guid id,
            SetTrackPublishedRequest request,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new SetTrackPublishedCommand(currentUser.RequiredId, id, request.IsPublished), cancellationToken);
            return result.ToHttpResult();
        }

        private static async Task<IResult> SetTracksPublished(
            IMediator mediator,
            CurrentUser currentUser,
            SetTracksPublishedRequest request,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(
                new SetTracksPublishedCommand(currentUser.RequiredId, request.TrackIds, request.IsPublished),
                cancellationToken);
            return result.ToHttpResult();
        }

        private static async Task<IResult> GetTrackStatusCounts(
            IMediator mediator,
            CurrentUser currentUser,
            long userId,
            CancellationToken cancellationToken)
        {
            if (currentUser.RequiredId != userId)
                return Results.Problem("You can only read the status counts of your own tracks.", statusCode: StatusCodes.Status403Forbidden);

            var result = await mediator.Send(new GetTrackStatusCountsQuery(userId), cancellationToken);
            return Results.Ok(result);
        }

        private static async Task<IResult> DeleteTracks(
            IMediator mediator,
            CurrentUser currentUser,
            [FromBody] DeleteTracksRequest request,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new DeleteTracksCommand(currentUser.RequiredId, request.TrackIds), cancellationToken);
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
                    request.ExpectedAudioSizeBytes,
                    request.Audios?.Select(audio => new AudioUploadSpec(audio.FileType, audio.ContentType, audio.ExpectedSizeBytes)).ToList()),
                cancellationToken);

            return result.ToHttpResult();
        }
    }
}
