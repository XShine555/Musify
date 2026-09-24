using System.ComponentModel.DataAnnotations;

namespace Musify.Domain.ValueObjects;

/// <summary>The picture file names of an album or playlist. Resized names stay null until processing finishes.</summary>
public class EntityPictures
{
    [Required]
    public required string OriginalName { get; set; }

    public string? SmallName { get; set; }

    public string? MediumName { get; set; }

    public string? LargeName { get; set; }

    public static EntityPictures Pending(string objectName) => new() { OriginalName = objectName };
}
