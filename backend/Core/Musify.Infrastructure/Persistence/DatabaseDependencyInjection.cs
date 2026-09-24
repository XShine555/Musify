using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Persistence
{
    public static class DatabaseDependencyInjection
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatedOptions<DatabaseConfiguration>(configuration);

            services.AddDbContext<Database>();

            services.AddScoped<IDatabase>(serviceProvider => serviceProvider.GetRequiredService<Database>());

            return services;
        }
    }
}
