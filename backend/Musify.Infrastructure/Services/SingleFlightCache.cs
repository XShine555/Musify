using System.Collections.Concurrent;
using ErrorOr;
using Microsoft.Extensions.Caching.Memory;

namespace Musify.Infrastructure.Services
{
    public sealed class SingleFlightCache(IMemoryCache cache)
    {
        private readonly ConcurrentDictionary<string, object> inFlight = new();

        public async Task<ErrorOr<TValue>> GetOrCreateAsync<TValue>(
            string key,
            TimeSpan duration,
            Func<Task<ErrorOr<TValue>>> factory)
        {
            if (duration <= TimeSpan.Zero)
                return await factory();

            if (cache.TryGetValue(key, out TValue? cached) && cached is not null)
                return cached;

            var pending = (Lazy<Task<ErrorOr<TValue>>>)inFlight.GetOrAdd(
                key,
                _ => new Lazy<Task<ErrorOr<TValue>>>(factory));

            try
            {
                var result = await pending.Value;

                if (!result.IsError)
                    cache.Set(key, result.Value, duration);

                return result;
            }
            finally
            {
                inFlight.TryRemove(key, out _);
            }
        }
    }
}
