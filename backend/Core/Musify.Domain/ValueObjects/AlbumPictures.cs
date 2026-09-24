using System.ComponentModel.DataAnnotations;

namespace Musify.Domain.ValueObjects;

public class AlbumPictures
{
    [Required]
    public required string OriginalName { get; set; }

    public string? SmallName { get; set; }

    public string? MediumName { get; set; }

    public string? LargeName { get; set; }

    public static AlbumPictures Pending(string originalName) => new() { OriginalName = originalName };
}
