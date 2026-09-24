using Musify.Domain.ValueObjects;
using Xunit;

namespace Musify.Domain.Tests.ValueObjects;

public sealed class GenreCompatibilityTests
{
    [Theory]
    [InlineData(Genre.Classical, Genre.Metal)]
    [InlineData(Genre.Classical, Genre.Punk)]
    [InlineData(Genre.Ambient, Genre.Dubstep)]
    [InlineData(Genre.Lofi, Genre.DrumAndBass)]
    public void AreCompatible_IncompatiblePair_ReturnsFalse(Genre first, Genre second)
    {
        Assert.False(GenreCompatibility.AreCompatible(first, second));
        Assert.False(GenreCompatibility.AreCompatible(second, first));
    }

    [Theory]
    [InlineData(Genre.Pop, Genre.Rock)]
    [InlineData(Genre.HipHop, Genre.RnB)]
    [InlineData(Genre.Classical, Genre.Jazz)]
    public void AreCompatible_CompatiblePair_ReturnsTrue(Genre first, Genre second)
    {
        Assert.True(GenreCompatibility.AreCompatible(first, second));
    }

    [Fact]
    public void AreCompatible_SameGenre_ReturnsTrue()
    {
        Assert.True(GenreCompatibility.AreCompatible(Genre.Pop, Genre.Pop));
    }

    [Fact]
    public void FindConflicts_NoIncompatiblePairs_ReturnsEmpty()
    {
        var conflicts = GenreCompatibility.FindConflicts([Genre.Pop, Genre.Rock, Genre.Indie]);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void FindConflicts_IncompatiblePairPresent_ReturnsIt()
    {
        var conflicts = GenreCompatibility.FindConflicts([Genre.Pop, Genre.Classical, Genre.Metal]);

        var conflict = Assert.Single(conflicts);
        Assert.Equal((Genre.Classical, Genre.Metal), conflict);
    }
}
