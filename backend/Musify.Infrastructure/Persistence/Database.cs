using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MassTransit;
using Musify.Domain.Entities;
using Musify.Infrastructure.Configuration;
using Musify.Infrastructure.MassTransit.Sagas;
using AppIDatabase = Musify.Application.Contracts.IDatabase;
using Musify.Application.Contracts;

namespace Musify.Infrastructure.Persistence
{
    public class Database(DatabaseConfiguration configuration)
        : DbContext, AppIDatabase
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(configuration.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            var trackProcessing = modelBuilder.Entity<TrackProcessingState>();
            trackProcessing.HasKey(state => state.CorrelationId);
            trackProcessing.Property(state => state.CorrelationId).ValueGeneratedNever();
            trackProcessing.Property(state => state.CurrentState).HasMaxLength(64);

            var playListProcessing = modelBuilder.Entity<PlayListProcessingState>();
            playListProcessing.HasKey(state => state.CorrelationId);
            playListProcessing.Property(state => state.CorrelationId).ValueGeneratedNever();
            playListProcessing.Property(state => state.CurrentState).HasMaxLength(64);
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<PlayList> PlayLists => Set<PlayList>();

        public DbSet<UserHasTrack> UserHasTracks => Set<UserHasTrack>();

        public DbSet<PlayListHasTrack> PlayListHasTracks => Set<PlayListHasTrack>();

        public DbSet<Upload> Uploads => Set<Upload>();

        public DbSet<UploadIntent> UploadIntents => Set<UploadIntent>();

        public DbSet<ListeningHistory> ListeningHistories => Set<ListeningHistory>();

        public async Task<IDatabaseTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return new DatabaseTransaction(transaction);
        }

        private sealed class DatabaseTransaction(IDbContextTransaction inner) : IDatabaseTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken) =>
                inner.CommitAsync(cancellationToken);

            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }
    }
}
