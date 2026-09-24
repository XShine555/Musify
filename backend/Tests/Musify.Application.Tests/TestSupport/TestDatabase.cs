using ErrorOr;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Infrastructure.Persistence;

namespace Musify.Application.Tests.TestSupport;

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

        ModelConfiguration.Apply(modelBuilder);
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<PlayList> PlayLists => Set<PlayList>();

    public DbSet<Album> Albums => Set<Album>();

    public DbSet<AlbumHasTrack> AlbumHasTracks => Set<AlbumHasTrack>();

    public DbSet<Track> Tracks => Set<Track>();

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
        return new SqliteDatabaseTransaction(transaction);
    }

    public async Task<ErrorOr<Success>> TrySaveChangesAsync(Error onUniqueViolation, CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return onUniqueViolation;
        }
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
