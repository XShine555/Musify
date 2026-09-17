using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Tests.TestSupport
{
    public sealed class TestDatabase : DbContext, IDatabase
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, DbContextOptions<TestDatabase> options)
            : base(options)
        {
            this.connection = connection;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<TestDatabase>()
                .UseSqlite(connection)
                .Options;

            var database = new TestDatabase(connection, options);
            await database.Database.EnsureCreatedAsync();
            return database;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Track>().OwnsOne(track => track.Pictures);
            modelBuilder.Entity<Track>().Navigation(track => track.Pictures).IsRequired();

            modelBuilder.Entity<Track>().OwnsOne(track => track.Audio);
            modelBuilder.Entity<Track>().Navigation(track => track.Audio).IsRequired();

            modelBuilder.Entity<Track>()
                .HasOne(track => track.Owner)
                .WithMany()
                .HasForeignKey(track => track.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlayList>().OwnsOne(playList => playList.Pictures);
            modelBuilder.Entity<PlayList>().Navigation(playList => playList.Pictures).IsRequired();

            modelBuilder.Entity<Album>().OwnsOne(album => album.Pictures);
            modelBuilder.Entity<Album>().Navigation(album => album.Pictures).IsRequired(false);

            modelBuilder.Entity<Album>()
                .HasOne(album => album.Owner)
                .WithMany()
                .HasForeignKey(album => album.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlbumHasTrack>()
                .HasOne(albumTrack => albumTrack.Album)
                .WithMany(album => album.AlbumTracks)
                .HasForeignKey(albumTrack => albumTrack.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AlbumHasTrack>()
                .HasOne(albumTrack => albumTrack.Track)
                .WithMany(track => track.AlbumTracks)
                .HasForeignKey(albumTrack => albumTrack.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AlbumHasTrack>()
                .HasIndex(albumTrack => new { albumTrack.AlbumId, albumTrack.TrackId })
                .IsUnique();

            modelBuilder.Entity<Mix>()
                .HasIndex(mix => new { mix.UserId, mix.Position });

            modelBuilder.Entity<MixItem>()
                .HasOne(item => item.Mix)
                .WithMany(mix => mix.Items)
                .HasForeignKey(item => item.MixId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MixItem>()
                .HasOne(item => item.Track)
                .WithMany()
                .HasForeignKey(item => item.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MixItem>()
                .HasIndex(item => new { item.MixId, item.Position });

            modelBuilder.Entity<TrackLike>()
                .HasIndex(like => new { like.UserId, like.TrackId })
                .IsUnique();

            modelBuilder.Entity<TrackTag>()
                .HasOne(trackTag => trackTag.Track)
                .WithMany(track => track.Tags)
                .HasForeignKey(trackTag => trackTag.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrackTag>()
                .HasIndex(trackTag => new { trackTag.TrackId, trackTag.Tag })
                .IsUnique();

            modelBuilder.Entity<UserFollow>()
                .HasOne(follow => follow.Follower)
                .WithMany()
                .HasForeignKey(follow => follow.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollow>()
                .HasOne(follow => follow.Followed)
                .WithMany()
                .HasForeignKey(follow => follow.FollowedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollow>()
                .HasIndex(follow => new { follow.FollowerId, follow.FollowedId })
                .IsUnique();
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<PlayList> PlayLists => Set<PlayList>();

        public DbSet<Album> Albums => Set<Album>();

        public DbSet<AlbumHasTrack> AlbumHasTracks => Set<AlbumHasTrack>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<UserHasTrack> UserHasTracks => Set<UserHasTrack>();

        public DbSet<PlayListHasTrack> PlayListHasTracks => Set<PlayListHasTrack>();

        public DbSet<Mix> Mixes => Set<Mix>();

        public DbSet<MixItem> MixItems => Set<MixItem>();

        public DbSet<Upload> Uploads => Set<Upload>();

        public DbSet<UploadIntent> UploadIntents => Set<UploadIntent>();

        public DbSet<ListeningHistory> ListeningHistories => Set<ListeningHistory>();

        public DbSet<TrackLike> TrackLikes => Set<TrackLike>();

        public DbSet<TrackTag> TrackTags => Set<TrackTag>();

        public DbSet<UserFollow> UserFollows => Set<UserFollow>();

        public async Task<IDatabaseTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return new SqliteDatabaseTransaction(transaction);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await connection.DisposeAsync();
        }

        private sealed class SqliteDatabaseTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction inner)
            : IDatabaseTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken = default) =>
                inner.CommitAsync(cancellationToken);

            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }
    }
}
