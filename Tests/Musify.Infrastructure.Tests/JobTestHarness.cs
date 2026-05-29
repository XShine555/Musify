using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts;
using Musify.Application.Tests.Infrastructure;

namespace Musify.Infrastructure.Tests;

internal static class JobTestHarness
{
    public static IServiceScopeFactory ScopeFactoryWith(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    public static IServiceScopeFactory WithDatabase(IDatabase database) =>
        ScopeFactoryWith(s => s.AddSingleton(database));

    public static IServiceScopeFactory WithStorage(IStorageService storage) =>
        ScopeFactoryWith(s => s.AddSingleton(storage));

    public static Task RunAsync(object job, CancellationToken cancellationToken = default)
    {
        var method = job.GetType().GetMethod(
            "RunAsync",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("RunAsync not found on job.");

        return (Task)method.Invoke(job, [cancellationToken])!;
    }
}
