using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class PlayListConfiguration
    {
        public const string SectionName = "PlayListConfiguration";

        [Required]
        public PlayListRoutes Routes { get; set; } = new PlayListRoutes();

        [Required]
        public PlayListPicturesSizes PicturesSizes { get; set; } = new PlayListPicturesSizes();
    }

    public class PlayListRoutes
    {
        [Required]
        public string ParentFolders { get; set; } = "PlayLists";

        [Required]
        public string OriginalPictures { get; set; } = "OriginalPictures";

        [Required]
        public string SmallPictures { get; set; } = "SmallPictures";

        [Required]
        public string MediumPictures { get; set; } = "MediumPictures";

        [Required]
        public string LargePictures { get; set; } = "LargePictures";

        [Required]
        public string PresetOriginalPicture { get; set; } = "PresetOriginalPicture.webp";

        [Required]
        public string PresetSmallPicture { get; set; } = "PresetSmallPicture.webp";

        [Required]
        public string PresetMediumPicture { get; set; } = "PresetMediumPicture.webp";

        [Required]
        public string PresetLargePicture { get; set; } = "PresetLargePicture.webp";
    }

    public class PlayListPicturesSizes
    {
        [Range(1, 1024)]
        public int SmallPictureWidth { get; set; } = 128;

        [Range(1, 1024)]
        public int SmallPictureHeight { get; set; } = 128;

        [Range(1, 1024)]
        public int MediumPictureWidth { get; set; } = 256;

        [Range(1, 1024)]
        public int MediumPictureHeight { get; set; } = 256;

        [Range(1, 1024)]
        public int LargePictureWidth { get; set; } = 512;

        [Range(1, 1024)]
        public int LargePictureHeight { get; set; } = 512;
    }
}