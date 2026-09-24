using System.Diagnostics.CodeAnalysis;

namespace Musify.Domain.ValueObjects
{
    public enum AlbumType
    {
        Album = 0,
        EP = 1,
        [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Part of the public API contract.")]
        Single = 2
    }
}
