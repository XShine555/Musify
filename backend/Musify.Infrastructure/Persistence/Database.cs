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

            modelBuilder.Entity<Track>()
                .HasIndex(track => new { track.Source, track.ExternalId })
                .IsUnique();

            modelBuilder.Entity<Track>()
                .HasOne(track => track.Owner)
                .WithMany()
                .HasForeignKey(track => track.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Track>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Tracks_OwnerUserId_Source",
                    "(\"OwnerUserId\" IS NOT NULL) = (\"Source\" = 0)"));

            modelBuilder.Entity<Track>()
                .OwnsOne(track => track.Pictures, pictures =>
                {
                    pictures.Property(p => p.OriginalName).HasColumnName("OriginalPictureName").HasMaxLength(64);
                    pictures.Property(p => p.SmallName).HasColumnName("SmallPictureName").HasMaxLength(64);
                    pictures.Property(p => p.MediumName).HasColumnName("MediumPictureName").HasMaxLength(64);
                    pictures.Property(p => p.LargeName).HasColumnName("LargePictureName").HasMaxLength(64);
                    pictures.Property(p => p.ProcessingStatus).HasColumnName("PicturesProcessingStatus");
                });
            modelBuilder.Entity<Track>().Navigation(track => track.Pictures).IsRequired();

            modelBuilder.Entity<Track>()
                .OwnsOne(track => track.Audio, audio =>
                {
                    audio.Property(a => a.OriginalName).HasColumnName("OriginalAudioName").HasMaxLength(64);
                    audio.Property(a => a.FolderName).HasColumnName("AudioFolderName").HasMaxLength(64);
                    audio.Property(a => a.TranscodeStatus).HasColumnName("AudioTranscodeProcessingStatus");
                    audio.Property(a => a.DownloadRequested).HasColumnName("DownloadRequested");
                    audio.Property(a => a.RetryCount).HasColumnName("RetryCount");
                    audio.Property(a => a.LastRetryAt).HasColumnName("LastRetryAt");
                });
            modelBuilder.Entity<Track>().Navigation(track => track.Audio).IsRequired();

            modelBuilder.Entity<TrackArtist>()
                .HasKey(trackArtist => new { trackArtist.TrackId, trackArtist.ArtistId });

            modelBuilder.Entity<TrackArtist>()
                .HasOne(trackArtist => trackArtist.Track)
                .WithMany(track => track.TrackArtists)
                .HasForeignKey(trackArtist => trackArtist.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrackArtist>()
                .HasOne(trackArtist => trackArtist.Artist)
                .WithMany(artist => artist.TrackArtists)
                .HasForeignKey(trackArtist => trackArtist.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Artist>()
                .HasIndex(artist => artist.UserId)
                .IsUnique();

            modelBuilder.Entity<Artist>()
                .HasIndex(artist => artist.ExternalId)
                .IsUnique();

            modelBuilder.Entity<Artist>()
                .HasIndex(artist => artist.NormalizedName)
                .IsUnique()
                .HasFilter("\"ExternalId\" IS NULL AND \"UserId\" IS NULL");

            var playListProcessing = modelBuilder.Entity<PlayListProcessingState>();
            playListProcessing.HasKey(state => state.CorrelationId);
            playListProcessing.Property(state => state.CorrelationId).ValueGeneratedNever();
            playListProcessing.Property(state => state.CurrentState).HasMaxLength(64);
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<Artist> Artists => Set<Artist>();

        public DbSet<TrackArtist> TrackArtists => Set<TrackArtist>();

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
