using System.ComponentModel.DataAnnotations;

namespace Musify.Domain.ValueObjects;

public class AlbumPictures
{
    [Required]
    public required string OriginalName { get; set; }

    [Required]
    public required string SmallName { get; set; }

    [Required]
    public required string MediumName { get; set; }

    [Required]
    public required string LargeName { get; set; }
}
