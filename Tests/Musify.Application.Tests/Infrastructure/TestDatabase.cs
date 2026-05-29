using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Musify.Domain.Entities;
using AppIDatabase = Musify.Application.Contracts.IDatabase;
using IDatabaseTransaction = Musify.Application.Contracts.IDatabaseTransaction;

namespace Musify.Application.Tests.Infrastructure;

public sealed class TestDatabase(DbContextOptions<TestDatabase> options) : DbContext(options), AppIDatabase
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Track> Tracks => Set<Track>();

    public DbSet<PlayList> PlayLists => Set<PlayList>();

    public DbSet<UserHasTrack> UserHasTracks => Set<UserHasTrack>();

    public DbSet<PlayListHasTrack> PlayListHasTracks => Set<PlayListHasTrack>();

    public DbSet<Upload> Uploads => Set<Upload>();

    public DbSet<UploadIntent> UploadIntents => Set<UploadIntent>();

    public async Task<IDatabaseTransaction> BeginTransactionAsync(
        System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new TestDatabaseTransaction(transaction);
    }

    private sealed class TestDatabaseTransaction(IDbContextTransaction inner) : IDatabaseTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            inner.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
