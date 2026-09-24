using Musify.Infrastructure.Jobs;
using Xunit;

namespace Musify.Infrastructure.Tests.Jobs;

public sealed class RecurringJobsRegistrarTests
{
    [Theory]
    [InlineData(1, "*/1 * * * *")]
    [InlineData(59, "*/1 * * * *")]
    [InlineData(60, "*/1 * * * *")]
    [InlineData(61, "*/2 * * * *")]
    [InlineData(300, "*/5 * * * *")]
    [InlineData(3599, "0 */1 * * *")]
    [InlineData(3600, "0 */1 * * *")]
    [InlineData(7200, "0 */2 * * *")]
    [InlineData(0, "*/1 * * * *")]
    public void EveryCron_RoundsUpToWholeMinutes(int seconds, string expected)
    {
        Assert.Equal(expected, RecurringJobsRegistrar.EveryCron(seconds));
    }
}
