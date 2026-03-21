using Microsoft.Extensions.DependencyInjection;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Infrastructure.Audio
{
    public static class AudioTranscoderDependencyInjection
    {
        public static IServiceCollection AddAudioTranscoder(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddScoped<IAudioTranscoder, AudioTranscoder>();
            return serviceDescriptors;
        }
    }
}