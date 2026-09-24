using System.ComponentModel.DataAnnotations;

namespace Musify.Domain.ValueObjects;

public class PlayListPictures
{
    [Required]
    public required string OriginalName { get; set; }

    public string? SmallName { get; set; }

    public string? MediumName { get; set; }

    public string? LargeName { get; set; }

    public static PlayListPictures Pending(string originalName) => new() { OriginalName = originalName };
}
