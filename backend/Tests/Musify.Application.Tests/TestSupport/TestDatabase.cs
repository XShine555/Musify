using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Tests.TestSupport;

/// <summary>
/// An <see cref="IDatabase"/> backed by a private, in-memory SQLite database.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IDatabase"/> exposes raw <see cref="DbSet{TEntity}"/> properties, so handlers
/// compose real LINQ queries (<c>Where</c>, <c>Select</c>, <c>Include</c>, ...) against it.
/// Substituting <see cref="IDatabase"/> with a mock cannot express that — there is no
/// <see cref="IQueryable{T}"/> provider behind a mock. SQLite's in-memory mode gives us a real
/// relational engine instead: fast, no external dependency, and — unlike the EF Core InMemory
/// provider — it actually supports transactions, which two handlers rely on.
/// </para>
/// <para>
/// The model mirrors the parts of <c>Musify.Infrastructure.Persistence.Database</c> that affect
/// query shape (owned types, table-per-type inheritance, unique indexes). MassTransit outbox/saga
/// state and Postgres-specific configuration are left out — the Application layer never touches
/// them.
/// </para>
/// </remarks>
public sealed class TestDatabase : DbContext, IDatabase
{
    private readonly SqliteConnection connection;

    private TestDatabase(SqliteConnection connection, DbContextOptions<TestDatabase> options)
        : base(options)
    {
        this.connection = connection;
    }

    /// <summary>
    /// Creates a fresh, empty database with its own private connection. Call <see cref="Seed"/>
    /// (via <see cref="Seeding.EntitySeedExtensions"/>) to populate it, then <see cref="DisposeAsync"/>
    /// when the test is done.
    /// </summary>
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

        modelBuilder.Entity<LocalTrack>().ToTable("LocalTracks");

        modelBuilder.Entity<LocalTrack>()
            .HasOne(track => track.Owner)
            .WithMany()
            .HasForeignKey(track => track.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExternalTrack>().ToTable("ExternalTracks");

        modelBuilder.Entity<ExternalTrack>()
            .HasIndex(track => new { track.Source, track.ExternalId })
            .IsUnique();

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
            .HasIndex(artist => artist.ExternalId)
            .IsUnique();

        modelBuilder.Entity<PlayList>().OwnsOne(playList => playList.Pictures);
        modelBuilder.Entity<PlayList>().Navigation(playList => playList.Pictures).IsRequired();

        modelBuilder.Entity<Album>().OwnsOne(album => album.Pictures);
        modelBuilder.Entity<Album>().Navigation(album => album.Pictures).IsRequired(false);

        modelBuilder.Entity<UserAlbum>().ToTable("UserAlbums");

        modelBuilder.Entity<UserAlbum>()
            .HasOne(album => album.Owner)
            .WithMany()
            .HasForeignKey(album => album.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExternalAlbum>().ToTable("ExternalAlbums");

        modelBuilder.Entity<ExternalAlbum>()
            .HasIndex(album => new { album.Source, album.ExternalId })
            .IsUnique();

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
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MixItem>()
            .HasIndex(item => new { item.MixId, item.Position });
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<PlayList> PlayLists => Set<PlayList>();

    public DbSet<Album> Albums => Set<Album>();

    public DbSet<UserAlbum> UserAlbums => Set<UserAlbum>();

    public DbSet<ExternalAlbum> ExternalAlbums => Set<ExternalAlbum>();

    public DbSet<AlbumHasTrack> AlbumHasTracks => Set<AlbumHasTrack>();

    public DbSet<Track> Tracks => Set<Track>();

    public DbSet<LocalTrack> LocalTracks => Set<LocalTrack>();

    public DbSet<ExternalTrack> ExternalTracks => Set<ExternalTrack>();

    public DbSet<Artist> Artists => Set<Artist>();

    public DbSet<TrackArtist> TrackArtists => Set<TrackArtist>();

    public DbSet<UserHasTrack> UserHasTracks => Set<UserHasTrack>();

    public DbSet<PlayListHasTrack> PlayListHasTracks => Set<PlayListHasTrack>();

    public DbSet<Mix> Mixes => Set<Mix>();

    public DbSet<MixItem> MixItems => Set<MixItem>();

    public DbSet<Upload> Uploads => Set<Upload>();

    public DbSet<UploadIntent> UploadIntents => Set<UploadIntent>();

    public DbSet<ListeningHistory> ListeningHistories => Set<ListeningHistory>();

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
