using ErrorOr;
using Microsoft.Extensions.Caching.Memory;
using Musify.Infrastructure.Services;
using Xunit;

namespace Musify.Infrastructure.Tests.Services
{
    public sealed class SingleFlightCacheTests
    {
        private static SingleFlightCache CreateCache() => new(new MemoryCache(new MemoryCacheOptions()));

        [Fact]
        public async Task GetOrCreateAsync_SecondCallWithinDuration_ReturnsCachedValueWithoutCallingFactory()
        {
            var cache = CreateCache();
            var calls = 0;
            Task<ErrorOr<int>> Factory() { calls++; return Task.FromResult<ErrorOr<int>>(calls); }

            var first = await cache.GetOrCreateAsync("key", TimeSpan.FromMinutes(1), Factory);
            var second = await cache.GetOrCreateAsync("key", TimeSpan.FromMinutes(1), Factory);

            Assert.Equal(1, first.Value);
            Assert.Equal(1, second.Value);
            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task GetOrCreateAsync_ConcurrentCallsForTheSameKey_CollapseIntoASingleFactoryCall()
        {
            var cache = CreateCache();
            var calls = 0;
            var gate = new TaskCompletionSource();

            async Task<ErrorOr<int>> Factory()
            {
                Interlocked.Increment(ref calls);
                await gate.Task;
                return 7;
            }

            var first = cache.GetOrCreateAsync("concurrent-key", TimeSpan.FromMinutes(1), Factory);
            var second = cache.GetOrCreateAsync("concurrent-key", TimeSpan.FromMinutes(1), Factory);
            gate.SetResult();

            var results = await Task.WhenAll(first, second);

            Assert.Equal(1, calls);
            Assert.All(results, result => Assert.Equal(7, result.Value));
        }

        [Fact]
        public async Task GetOrCreateAsync_FactoryReturnsError_DoesNotCacheIt()
        {
            var cache = CreateCache();
            var calls = 0;
            Task<ErrorOr<int>> Factory()
            {
                calls++;
                return Task.FromResult<ErrorOr<int>>(Error.Failure(description: "boom"));
            }

            await cache.GetOrCreateAsync("error-key", TimeSpan.FromMinutes(1), Factory);
            await cache.GetOrCreateAsync("error-key", TimeSpan.FromMinutes(1), Factory);

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task GetOrCreateAsync_NonPositiveDuration_AlwaysCallsTheFactory()
        {
            var cache = CreateCache();
            var calls = 0;
            Task<ErrorOr<int>> Factory() { calls++; return Task.FromResult<ErrorOr<int>>(calls); }

            await cache.GetOrCreateAsync("no-cache-key", TimeSpan.Zero, Factory);
            await cache.GetOrCreateAsync("no-cache-key", TimeSpan.Zero, Factory);

            Assert.Equal(2, calls);
        }
    }
}
