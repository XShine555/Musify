using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration;

public class PlayListConfiguration : IConfigurationOptions, IPictureOwnerConfiguration
{
    public static string SectionName => "PlayList";

    [Required]
    public PictureRoutes Routes { get; set; } = new() { ParentFolder = "PlayLists" };

    [Required]
    public PictureSizes PicturesSizes { get; set; } = new();
}
