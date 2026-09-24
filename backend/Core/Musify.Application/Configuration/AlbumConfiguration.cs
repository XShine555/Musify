using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class AlbumConfiguration : IConfigurationOptions, IPictureOwnerConfiguration
    {
        public static string SectionName => "Album";

        [Required]
        public PictureRoutes Routes { get; set; } = new() { ParentFolder = "Albums" };

        [Required]
        public PictureSizes PicturesSizes { get; set; } = new();
    }
}
