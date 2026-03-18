using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Infrastructure.Pictures
{
    public static class PictureDependencyInjection
    {
        public static void AddPictureHandler(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddScoped<IPictureHandler, PictureHandler>();
        }
    }
}