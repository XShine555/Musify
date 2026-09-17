using Microsoft.EntityFrameworkCore;
using Musify.Domain.Entities;

namespace Musify.Application.Contracts
{
    public interface IDatabase
    {
        DbSet<User> Users { get; }

        DbSet<PlayList> PlayLists { get; }

        DbSet<Album> Albums { get; }

        DbSet<AlbumHasTrack> AlbumHasTracks { get; }

        DbSet<Track> Tracks { get; }

        DbSet<UserHasTrack> UserHasTracks { get; }

        DbSet<PlayListHasTrack> PlayListHasTracks { get; }

        DbSet<Mix> Mixes { get; }

        DbSet<MixItem> MixItems { get; }

        DbSet<Upload> Uploads { get; }

        DbSet<UploadIntent> UploadIntents { get; }

        DbSet<ListeningHistory> ListeningHistories { get; }

        DbSet<TrackLike> TrackLikes { get; }

        DbSet<TrackTag> TrackTags { get; }

        DbSet<UserFollow> UserFollows { get; }

        Task<IDatabaseTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}