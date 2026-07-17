using Microsoft.EntityFrameworkCore;
using Musify.Domain.Entities;

namespace Musify.Application.Contracts
{
    public interface IDatabase
    {
        DbSet<User> Users { get; }

        DbSet<PlayList> PlayLists { get; }

        DbSet<Track> Tracks { get; }

        DbSet<Artist> Artists { get; }

        DbSet<TrackArtist> TrackArtists { get; }

        DbSet<UserHasTrack> UserHasTracks { get; }

        DbSet<PlayListHasTrack> PlayListHasTracks { get; }

        DbSet<Upload> Uploads { get; }

        DbSet<UploadIntent> UploadIntents { get; }

        DbSet<ListeningHistory> ListeningHistories { get; }

        Task<IDatabaseTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}