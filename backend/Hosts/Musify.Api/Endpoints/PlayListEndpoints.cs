using Mediator;
using Musify.Api.Authentication;
using Musify.Api.DataTransferObjects;
using Musify.Api.DataTransferObjects.PlayLists;
using Musify.Api.Extensions;
using Musify.Api.Filters;
using Musify.Api.Models;
using Musify.Application.Contracts;
using Musify.Application.Pictures;
using Musify.Application.Pictures.Responses;
using Musify.Application.PlayLists;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;
using Musify.Application.Tracks.Responses;

namespace Musify.Api.Endpoints
{
    public static class PlayListEndpoints
    {
        public static IEndpointRouteBuilder MapPlayListEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/playlists")
                .WithTags("PlayLists");

            group.MapGet("/{id:guid}", GetPlayListById)
                .WithName("GetPlayListById")
                .WithSummary("Get a playlist by id")
                .Produces<PlayListApplicationResponse>()
                .Produces(StatusCodes.Status404NotFound);

            group.MapGet("/users/{userId:long}", GetPlayListsByUserId)
                .AddEndpointFilter<ValidationFilter<PageQuery>>()
                .WithName("GetPlayListsByUserId")
                .WithSummary("Get paginated playlists for a user. Private playlists are only returned to their owner")
                .Produces<PaginatedResponse<PlayListApplicationResponse>>();

            group.MapGet("/{id:guid}/tracks", GetPlayListTracks)
                .AddEndpointFilter<ValidationFilter<PageQuery>>()
                .WithName("GetPlayListTracks")
                .WithSummary("Get paginated tracks of a playlist")
                .Produces<PaginatedResponse<TrackApplicationResponse>>()
                .Produces(StatusCodes.Status404NotFound);

            group.MapGet("/{id:guid}/cover", GetPlayListCover)
                .WithName("GetPlayListCover")
                .WithSummary("Get a playlist cover image")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);

            group.MapPost("/", CreatePlayList)
                .WithName("CreatePlayList")
                .WithSummary("Create a new playlist")
                .AddEndpointFilter<ValidationFilter<CreatePlayListRequest>>()
                .RequireAuthorization()
                .Produces<PlayListApplicationResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound);

            group.MapPut("/{id:guid}", UpdatePlayList)
                .WithName("UpdatePlayList")
                .WithSummary("Update an existing playlist")
                .AddEndpointFilter<ValidationFilter<UpdatePlayListRequest>>()
                .RequireAuthorization()
                .Produces<PlayListApplicationResponse>()
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);

            group.MapDelete("/{id:guid}", DeletePlayList)
                .WithName("DeletePlayList")
                .WithSummary("Delete a playlist")
                .RequireAuthorization()
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound);

            group.MapPost("/upload-picture", RequestPlayListPictureUpload)
                .WithName("RequestPlayListPictureUpload")
                .WithSummary("Request a pre-signed URL to upload a playlist picture")
                .AddEndpointFilter<ValidationFilter<PictureUploadRequest>>()
                .RequireAuthorization()
                .Produces<PictureUploadResponse>()
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status404NotFound);

            group.MapPost("/{id:guid}/tracks", AddPlayListTracks)
                .WithName("AddPlayListTracks")
                .WithSummary("Add tracks to a playlist, skipping the ones it already has")
                .AddEndpointFilter<ValidationFilter<AddTracksRequest>>()
                .RequireAuthorization()
                .Produces(StatusCodes.Status204NoContent)
                .ProducesValidationProblem()
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status409Conflict);

            group.MapDelete("/{id:guid}/tracks/{trackId:guid}", RemoveTrackFromPlayList)
                .WithName("RemoveTrackFromPlayList")
                .WithSummary("Remove a track from a playlist")
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
            string? name,
            [AsParameters] PageQuery page,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetPlayListsByUserIdQuery(userId, name, page.PageNumber, page.PageSize, currentUser.Id), cancellationToken);
            return Results.Ok(result);
        }

        private static async Task<IResult> GetPlayListTracks(
            IMediator mediator,
            CurrentUser currentUser,
            Guid id,
            [AsParameters] PageQuery page,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetPlayListTracksQuery(id, page.PageNumber, page.PageSize, currentUser.Id), cancellationToken);
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
            Guid id,
            UpdatePlayListRequest request,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(
                new UpdatePlayListCommand(currentUser.RequiredId, id, request.Name, request.Description, request.PictureIntentId, request.Visibility),
                cancellationToken);

            return result.ToHttpResult();
        }

        private static async Task<IResult> DeletePlayList(
            IMediator mediator,
            CurrentUser currentUser,
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new DeletePlayListCommand(currentUser.RequiredId, id), cancellationToken);
            return result.ToHttpResult();
        }

        private static async Task<IResult> RequestPlayListPictureUpload(
            IMediator mediator,
            CurrentUser currentUser,
            PictureUploadRequest request,
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
            Guid id,
            Guid trackId,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(
                new RemoveTrackFromPlayListCommand(currentUser.RequiredId, id, trackId),
                cancellationToken);

            return result.ToHttpResult();
        }
    }
}
