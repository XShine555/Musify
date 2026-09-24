using System.ComponentModel.DataAnnotations;

namespace Musify.Domain.ValueObjects
{
    public class EntityPictures
    {
        [Required]
        public required string OriginalName { get; set; }

        public string? SmallName { get; set; }

        public string? MediumName { get; set; }

        public string? LargeName { get; set; }

        public static EntityPictures Pending(string objectName) => new() { OriginalName = objectName };
    }
}
