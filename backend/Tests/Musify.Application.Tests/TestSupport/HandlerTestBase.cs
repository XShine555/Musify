using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Musify.Application.Tests.TestSupport;

/// <summary>
/// Base class for handler tests: gives every test method its own empty, isolated
/// <see cref="TestDatabase"/> (xUnit creates a new instance of the test class per test method,
/// so there is no state to reset between tests) and a no-op logger factory for handlers that
/// take an <see cref="ILogger{TCategoryName}"/>.
/// </summary>
public abstract class HandlerTestBase : IAsyncLifetime
{
    private TestDatabase database = null!;

    protected TestDatabase Database => database;

    public async Task InitializeAsync() => database = await TestDatabase.CreateAsync();

    public Task DisposeAsync() => database.DisposeAsync().AsTask();

    protected static ILogger<T> NoOpLogger<T>() => NullLogger<T>.Instance;

    /// <summary>Adds the entities and persists them, so handlers under test see committed data.</summary>
    protected async Task SeedAsync(params object[] entities)
    {
        foreach (var entity in entities)
            Database.Add(entity);

        await Database.SaveChangesAsync(CancellationToken.None);
    }
}
