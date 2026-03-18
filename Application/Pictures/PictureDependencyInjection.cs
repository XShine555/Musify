using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Application;

namespace Musify.Application.Pictures
{
    public static class PictureDependencyInjection
    {
        public static void AddPictureService(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddScoped<IPictureService, PictureService>();
        }
    }
}