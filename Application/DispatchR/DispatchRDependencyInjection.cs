using DispatchR.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Musify.Application.DispatchR
{
    public static class DispatchRDependencyInjection
    {
        public static void AddDispatchR(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddDispatchR(typeof(DispatchRDependencyInjection).Assembly);
        }
    }
}