using ErrorOr;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.Configuration;
using Npgsql;

namespace Musify.Infrastructure.Persistence
{
    public class Database(DatabaseConfiguration configuration)
        : DbContext, Musify.Application.Contracts.IDatabase
    {
        private static readonly AuditableEntityInterceptor AuditInterceptor = new();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseNpgsql(configuration.ConnectionString, o => o.MapEnum<Genre>("genre"))
                .AddInterceptors(AuditInterceptor);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            ModelConfiguration.Apply(modelBuilder);
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<PlayList> PlayLists => Set<PlayList>();

        public DbSet<Album> Albums => Set<Album>();

        public DbSet<AlbumHasTrack> AlbumHasTracks => Set<AlbumHasTrack>();

        public DbSet<PlayListHasTrack> PlayListHasTracks => Set<PlayListHasTrack>();

        public DbSet<Mix> Mixes => Set<Mix>();

        public DbSet<MixItem> MixItems => Set<MixItem>();

        public DbSet<UploadIntent> UploadIntents => Set<UploadIntent>();

        public DbSet<ListeningHistory> ListeningHistories => Set<ListeningHistory>();

        public DbSet<TrackLike> TrackLikes => Set<TrackLike>();

        public DbSet<TrackTag> TrackTags => Set<TrackTag>();

        public DbSet<UserFollow> UserFollows => Set<UserFollow>();

        public async Task<IDatabaseTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return new DatabaseTransaction(transaction);
        }

        public async Task<ErrorOr<Success>> TrySaveChangesAsync(Error onUniqueViolation, CancellationToken cancellationToken)
        {
            try
            {
                await SaveChangesAsync(cancellationToken);
                return Result.Success;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return onUniqueViolation;
            }
        }

        private sealed class DatabaseTransaction(IDbContextTransaction inner) : IDatabaseTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken) =>
                inner.CommitAsync(cancellationToken);

            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }
    }
}
