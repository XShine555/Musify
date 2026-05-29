using Mediator;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Queries;
using WebApi.DataTransferObjects.Tracks;
using WebApi.Extensions;
using WebApi.Filters;

namespace WebApi.Endpoints;

public static class TrackEndpoints
{
    public static IEndpointRouteBuilder MapTrackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tracks")
            .WithTags("Tracks");

        group.MapGet("/", GetTracks)
            .WithName("GetTracks")
            .WithSummary("Get Paginated Tracks.");

        group.MapGet("/{id}", GetTrackById)
            .WithName("GetTrackById")
            .WithSummary("Get A Track By Id.");

        group.MapGet("/users/{userId}", GetTracksByUserId)
            .WithName("GetTracksByUserId")
            .WithSummary("Get Paginated Tracks For A User.");

        group.MapPost("/users/{userId}", CreateTrack)
            .WithName("CreateTrack")
            .WithSummary("Create A New Track.")
            .AddEndpointFilter<ValidationFilter<CreateTrackRequest>>();

        group.MapDelete("/{trackId}/users/{userId}", DeleteTrack)
            .WithName("DeleteTrack")
            .WithSummary("Delete A Track.");

        group.MapPost("/upload-urls/users/{userId}", RequestTrackUploadUrls)
            .WithName("RequestTrackUploadUrls")
            .WithSummary("Request Pre-Signed URLs To Upload Track Picture And Audio.")
            .AddEndpointFilter<ValidationFilter<RequestTrackUploadUrlsRequest>>();

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
        string? name,
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

        return Results.Created($"/tracks/{result.Value.Id}", result.Value);
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
