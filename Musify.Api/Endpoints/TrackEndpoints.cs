using Mediator;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Queries;
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
            .WithSummary("Get Paginated Tracks.");

        group.MapGet("/{id}", GetTrackById)
            .WithName("GetTrackById")
            .WithSummary("Get A Track By Id.");

        group.MapGet("/{id}/stream", GetTrackStream)
            .WithName("GetTrackStream")
            .WithSummary("Get A Streaming Manifest URL And Ticket For A Track.")
            .RequireAuthorization();

        group.MapGet("/users/{userId}", GetTracksByUserId)
            .WithName("GetTracksByUserId")
            .WithSummary("Get Paginated Tracks For A User.");

        group.MapPost("/", CreateTrack)
            .WithName("CreateTrack")
            .WithSummary("Create A New Track.")
            .AddEndpointFilter<ValidationFilter<CreateTrackRequest>>()
            .RequireAuthorization();

        group.MapDelete("/{trackId}", DeleteTrack)
            .WithName("DeleteTrack")
            .WithSummary("Delete A Track.")
            .RequireAuthorization();

        group.MapPost("/upload-urls", RequestTrackUploadUrls)
            .WithName("RequestTrackUploadUrls")
            .WithSummary("Request Pre-Signed URLs To Upload Track Picture And Audio.")
            .AddEndpointFilter<ValidationFilter<RequestTrackUploadUrlsRequest>>()
            .RequireAuthorization();

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

        if (!result.IsSuccess)
            return result.ToHttpResult();

        return Results.Created($"/tracks/{result.Value.Id}", result.Value);
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
