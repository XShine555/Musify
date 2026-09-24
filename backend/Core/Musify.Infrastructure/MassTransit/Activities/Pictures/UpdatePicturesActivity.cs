using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.Logs;

namespace Musify.Infrastructure.MassTransit.Activities.Pictures
{
    internal sealed record PictureNames(string? Original, string? Small, string? Medium, string? Large);

    /// <summary>Stores the processed picture names on the owner and reports it; compensation restores the previous names.</summary>
    internal abstract class UpdatePicturesActivity<TEntity>(
        IDatabase database,
        IPublishEndpoint publishEndpoint,
        ILogger logger)
        : IActivity<UpdatePicturesArguments, UpdatePicturesLog>
        where TEntity : class
    {
        protected IDatabase Database { get; } = database;

        protected abstract Task<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>Reads the current names; throws when the entity has no pending picture.</summary>
        protected abstract PictureNames Read(TEntity entity);

        protected abstract void Write(TEntity entity, PictureNames names, bool succeeded);

        protected abstract object ProcessedEvent(Guid id);

        public async Task<ExecutionResult> Execute(ExecuteContext<UpdatePicturesArguments> executeContext)
        {
            var arguments = executeContext.Arguments;
            var small = RequireVariable(executeContext, arguments.SmallPictureVariable);
            var medium = RequireVariable(executeContext, arguments.MediumPictureVariable);
            var large = RequireVariable(executeContext, arguments.LargePictureVariable);

            var entity = await FindAsync(arguments.SubjectId, executeContext.CancellationToken)
                ?? throw new InvalidOperationException($"{typeof(TEntity).Name} {arguments.SubjectId} not found");

            var previous = Read(entity);
            Write(
                entity,
                new PictureNames(
                    Path.GetFileName(arguments.OriginalPictureKey),
                    Path.GetFileName(small),
                    Path.GetFileName(medium),
                    Path.GetFileName(large)),
                succeeded: true);
            await Database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Updated {Entity} {Id} pictures", typeof(TEntity).Name, arguments.SubjectId);

            await publishEndpoint.Publish(ProcessedEvent(arguments.SubjectId), executeContext.CancellationToken);

            return executeContext.Completed(new UpdatePicturesLog(
                arguments.SubjectId, previous.Original, previous.Small, previous.Medium, previous.Large));
        }

        public async Task<CompensationResult> Compensate(CompensateContext<UpdatePicturesLog> compensateContext)
        {
            var log = compensateContext.Log;

            try
            {
                var entity = await FindAsync(log.SubjectId, compensateContext.CancellationToken);
                if (entity == null)
                    return compensateContext.Compensated();

                var previous = new PictureNames(log.PreviousOriginalName, log.PreviousSmallName, log.PreviousMediumName, log.PreviousLargeName);
                Write(entity, previous, succeeded: false);
                await Database.SaveChangesAsync(compensateContext.CancellationToken);

                return compensateContext.Compensated();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to compensate {Entity} {Id} pictures", typeof(TEntity).Name, log.SubjectId);
                return compensateContext.Failed(exception);
            }
        }

        protected static PictureNames ReadPictures(EntityPictures? pictures) =>
            pictures == null
                ? throw new InvalidOperationException("The entity has no pending picture upload")
                : new PictureNames(pictures.OriginalName, pictures.SmallName, pictures.MediumName, pictures.LargeName);

        protected static void WritePictures(EntityPictures? pictures, PictureNames names)
        {
            if (pictures == null)
                return;

            pictures.OriginalName = names.Original ?? pictures.OriginalName;
            pictures.SmallName = names.Small;
            pictures.MediumName = names.Medium;
            pictures.LargeName = names.Large;
        }

        private static string RequireVariable(ExecuteContext<UpdatePicturesArguments> executeContext, string variableName) =>
            executeContext.GetVariable<string>(variableName)
                ?? throw new InvalidOperationException($"Missing routing slip variable {variableName}");
    }

    internal sealed class UpdateTrackPictureActivity(IDatabase database, IPublishEndpoint publishEndpoint, ILogger<UpdateTrackPictureActivity> logger)
        : UpdatePicturesActivity<Track>(database, publishEndpoint, logger)
    {
        public const string ExecuteEndpointName = "update-track-picture";

        protected override Task<Track?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Database.Tracks.SingleOrDefaultAsync(track => track.Id == id, cancellationToken);

        protected override PictureNames Read(Track track) =>
            new(track.Pictures.OriginalName, track.Pictures.SmallName, track.Pictures.MediumName, track.Pictures.LargeName);

        protected override void Write(Track track, PictureNames names, bool succeeded)
        {
            track.Pictures.OriginalName = names.Original;
            track.Pictures.SmallName = names.Small;
            track.Pictures.MediumName = names.Medium;
            track.Pictures.LargeName = names.Large;
            track.Pictures.ProcessingStatus = succeeded ? ProcessingStatus.Completed : ProcessingStatus.Failed;
        }

        protected override object ProcessedEvent(Guid id) => new TrackPictureProcessed(id);
    }

    internal sealed class UpdatePlayListPictureActivity(IDatabase database, IPublishEndpoint publishEndpoint, ILogger<UpdatePlayListPictureActivity> logger)
        : UpdatePicturesActivity<PlayList>(database, publishEndpoint, logger)
    {
        public const string ExecuteEndpointName = "update-play-list-picture";

        protected override Task<PlayList?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Database.PlayLists.SingleOrDefaultAsync(playList => playList.Id == id, cancellationToken);

        protected override PictureNames Read(PlayList playList) => ReadPictures(playList.Pictures);

        protected override void Write(PlayList playList, PictureNames names, bool succeeded) =>
            WritePictures(playList.Pictures, names);

        protected override object ProcessedEvent(Guid id) => new PlayListPictureProcessed(id);
    }

    internal sealed class UpdateAlbumPictureActivity(IDatabase database, IPublishEndpoint publishEndpoint, ILogger<UpdateAlbumPictureActivity> logger)
        : UpdatePicturesActivity<Album>(database, publishEndpoint, logger)
    {
        public const string ExecuteEndpointName = "update-album-picture";

        protected override Task<Album?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Database.Albums.SingleOrDefaultAsync(album => album.Id == id, cancellationToken);

        protected override PictureNames Read(Album album) => ReadPictures(album.Pictures);

        protected override void Write(Album album, PictureNames names, bool succeeded) =>
            WritePictures(album.Pictures, names);

        protected override object ProcessedEvent(Guid id) => new AlbumPictureProcessed(id);
    }
}
