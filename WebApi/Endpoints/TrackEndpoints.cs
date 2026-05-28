using Mediator;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Queries;
using WebApi.DataTransferObjects.Tracks;
using WebApi.Extensions;

namespace WebApi.Endpoints;

public static class TrackEndpoints
{
    public static IEndpointRouteBuilder MapTrackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tracks")
            .WithTags("Tracks");

        group.MapGet("/", GetTracks)
            .WithName("GetTracks")
            .WithSummary("Get paginated tracks.");

        group.MapGet("/{id:guid}", GetTrackById)
            .WithName("GetTrackById")
            .WithSummary("Get a track by ID.");

        group.MapGet("/users/{userId:guid}", GetTracksByUserId)
            .WithName("GetTracksByUserId")
            .WithSummary("Get paginated tracks for a user.");

        group.MapPost("/users/{userId:guid}", CreateTrack)
            .WithName("CreateTrack")
            .WithSummary("Create a new track.");

        group.MapDelete("/{trackId:guid}/users/{userId:guid}", DeleteTrack)
            .WithName("DeleteTrack")
            .WithSummary("Delete a track.");

        group.MapPost("/upload-urls/users/{userId:guid}", RequestTrackUploadUrls)
            .WithName("RequestTrackUploadUrls")
            .WithSummary("Request pre-signed URLs to upload track picture and audio.");

        return app;
    }

    private static async Task<IResult> GetTracks(
        IMediator mediator,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetTracksQuery(pageNumber, pageSize), cancellationToken);
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

    private static async Task<IResult> GetTracksByUserId(
        IMediator mediator,
        Guid userId,
        CancellationToken cancellationToken,
        string? name = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var result = await mediator.Send(new GetTracksByUserIdQuery(userId, name, pageNumber, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateTrack(
        IMediator mediator,
        Guid userId,
        CreateTrackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTrackCommand(userId, request.Title, request.PictureIntentId, request.AudioIntentId),
            cancellationToken);

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created($"/api/tracks/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> DeleteTrack(
        IMediator mediator,
        Guid userId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTrackCommand(userId, trackId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RequestTrackUploadUrls(
        IMediator mediator,
        Guid userId,
        RequestTrackUploadUrlsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RequestTrackUploadUrlsCommand(
                userId,
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
