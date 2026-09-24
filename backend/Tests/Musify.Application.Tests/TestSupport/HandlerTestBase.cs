using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Musify.Application.Tests.TestSupport;

public abstract class HandlerTestBase : IAsyncLifetime
{
    private TestDatabase database = null!;

    protected TestDatabase Database => database;

    public async ValueTask InitializeAsync() => database = await TestDatabase.CreateAsync();

    public ValueTask DisposeAsync() => database.DisposeAsync();

    protected static ILogger<T> NoOpLogger<T>() => NullLogger<T>.Instance;

    protected async Task SeedAsync(params object[] entities)
    {
        foreach (var entity in entities)
            Database.Add(entity);

        await Database.SaveChangesAsync(CancellationToken.None);
    }
}
