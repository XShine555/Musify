using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Domain.Entities;

namespace Musify.Application.Contracts;

public interface IDatabase
{
    public DbSet<User> Users { get; }

    public DbSet<PlayList> PlayLists { get; }

    public DbSet<Album> Albums { get; }

    public DbSet<AlbumHasTrack> AlbumHasTracks { get; }

    public DbSet<Track> Tracks { get; }

    public DbSet<UserHasTrack> UserHasTracks { get; }

    public DbSet<PlayListHasTrack> PlayListHasTracks { get; }

    public DbSet<Mix> Mixes { get; }

    public DbSet<MixItem> MixItems { get; }

    public DbSet<Upload> Uploads { get; }

    public DbSet<UploadIntent> UploadIntents { get; }

    public DbSet<ListeningHistory> ListeningHistories { get; }

    public DbSet<TrackLike> TrackLikes { get; }

    public DbSet<TrackTag> TrackTags { get; }

    public DbSet<UserFollow> UserFollows { get; }

    public Task<IDatabaseTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    public Task<ErrorOr<Success>> TrySaveChangesAsync(Error onUniqueViolation, CancellationToken cancellationToken);
}
