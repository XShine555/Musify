using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Albums.Responses;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Pictures;
using Musify.Application.Services;
using Musify.Application.Shared;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums
{
    public record CreateAlbumCommand(
        long UserId,
        string Title,
        string? Description,
        int? ReleaseYear,
        Guid PictureIntentId,
        AlbumType Type = AlbumType.Album,
        IReadOnlyList<Guid>? TrackIds = null)
        : ICommand<ErrorOr<AlbumApplicationResponse>>;

    public class CreateAlbumCommandHandler(
        IEventBus eventBus,
        IDatabase database,
        UploadIntentValidator uploadIntentValidator,
        ILogger<CreateAlbumCommandHandler> logger,
        AlbumConfiguration albumConfiguration)
        : ICommandHandler<CreateAlbumCommand, ErrorOr<AlbumApplicationResponse>>
    {
        public async ValueTask<ErrorOr<AlbumApplicationResponse>> Handle(CreateAlbumCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == request.UserId, cancellationToken);
            if (!userExists)
                return AppErrors.NotFound("User", request.UserId);

            var trackIds = (request.TrackIds ?? []).Distinct().ToList();
            var tracks = await database.Tracks
                .AsNoTracking()
                .Where(track => trackIds.Contains(track.Id) && track.LifeCycleStatus == LifeCycleStatus.Active)
                .Select(track => new { track.Id, track.OwnerUserId, track.IsPublished })
                .ToListAsync(cancellationToken);

            var missing = trackIds.FirstOrDefault(id => tracks.All(track => track.Id != id));
            if (missing != Guid.Empty)
                return AppErrors.NotFound("Track", missing);

            var foreign = tracks.FirstOrDefault(track => track.OwnerUserId != request.UserId);
            if (foreign != null)
                return AppErrors.Forbidden("Track", foreign.Id);

            var prepared = await PictureSourceChange.PrepareAsync(
                uploadIntentValidator, request.PictureIntentId, request.UserId, UploadIntentPurpose.AlbumPicture, albumConfiguration, cancellationToken);
            if (prepared.IsError)
                return prepared.Errors;

            var picture = prepared.Value;
            var title = request.Title.Trim();

            var album = new Album
            {
                OwnerUserId = request.UserId,
                Title = title,
                NormalizedTitle = TextNormalizer.Normalize(title),
                Description = request.Description,
                ReleaseYear = request.ReleaseYear,
                Type = request.Type,
                Pictures = picture.Pictures
            };

            await database.Albums.AddAsync(album, cancellationToken);

            for (var index = 0; index < trackIds.Count; index++)
            {
                await database.AlbumHasTracks.AddAsync(
                    new AlbumHasTrack { AlbumId = album.Id, TrackId = trackIds[index], TrackNumber = index + 1 },
                    cancellationToken);
            }

            await eventBus.PublishAsync(
                new CreateAlbumResourcesEvent(
                    album.Id, picture.Intent.Id, picture.Intent.Bucket, picture.Intent.Key, picture.FinalKey, picture.Sizes),
                cancellationToken);

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created album {AlbumId} for user {UserId} with {Count} tracks", album.Id, request.UserId, trackIds.Count);

            var published = tracks.Where(track => track.IsPublished).Select(track => track.Id).ToHashSet();
            var visibleTrackIds = trackIds.Where(published.Contains).ToList();

            return AlbumApplicationResponse.FromEntity(
                album,
                visibleTrackIds.Count,
                [.. visibleTrackIds.Take(AlbumProjections.CoverTrackCount)]);
        }
    }
}
