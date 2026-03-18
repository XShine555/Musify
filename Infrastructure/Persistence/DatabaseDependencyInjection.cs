using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Infrastructure.Persistence
{
    public static class DatabaseDependencyInjection
    {
        public static void AddDatabase(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Default Connection connection string not found.");

            serviceDescriptors.AddDbContext<Database>(options =>
            {
                options.UseNpgsql(connectionString);
            } );

            serviceDescriptors.AddScoped<IDatabase>(serviceProvider => serviceProvider.GetRequiredService<Database>());
        }
    }
}