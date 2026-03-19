using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Infrastructure.Persistence
{
    public static class DatabaseDependencyInjection
    {
        public static IServiceCollection AddDatabase(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            serviceDescriptors.AddDbContext<Database>();
            serviceDescriptors.AddScoped<IDatabase>(serviceProvider => serviceProvider.GetRequiredService<Database>());

            return serviceDescriptors;
        }
    }
}