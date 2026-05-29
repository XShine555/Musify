using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Musify.Application.Tests.Infrastructure;

public sealed class SqliteTestContext : IAsyncDisposable, IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase Database { get; }

    public SqliteTestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestDatabase>()
            .UseSqlite(_connection)
            .Options;

        Database = new TestDatabase(options);
        Database.Database.EnsureCreated();
    }

    public TestDatabase NewContext()
    {
        var options = new DbContextOptionsBuilder<TestDatabase>()
            .UseSqlite(_connection)
            .Options;
        return new TestDatabase(options);
    }

    public void Dispose()
    {
        Database.Dispose();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Database.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
