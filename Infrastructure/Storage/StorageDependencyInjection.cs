using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Infrastructure.Storage
{
    public static class StorageDependencyInjection
    {
        public static void AddStorageHandler(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddScoped<IStorageHandler, StorageHandler>();
        }
    }
}