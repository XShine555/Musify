using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class PlayListConfiguration
    {
        public const string SectionName = "PlayListConfiguration";

        [Required]
        public Routes Routes { get; set; } = new Routes();

        [Required]
        public PicturesSizes PicturesSizes { get; set; } = new PicturesSizes();
    }

    public class Routes
    {
        [Required]
        public string OriginalPictures { get; set; } = "OriginalPictures";

        [Required]
        public string SmallPictures { get; set; } = "SmallPictures";

        [Required]
        public string MediumPictures { get; set; } = "MediumPictures";

        [Required]
        public string LargePictures { get; set; } = "LargePictures";

        [Required]
        public string PresetSmallPicture { get; set; } = "PresetSmallPicture.webp";

        [Required]
        public string PresetMediumPicture { get; set; } = "PresetMediumPicture.webp";

        [Required]
        public string PresetLargePicture { get; set; } = "PresetLargePicture.webp";
    }

    public class PicturesSizes
    {
        [Range(1, 1024)]
        public int SmallPictureWidth { get; set; } = 128;

        [Range(1, 1024)]
        public int SmallPictureHeight { get; set; } = 128;

        [Range(1, 1024)]
        public int MediumPictureWidth { get; private set; } = 256;

        [Range(1, 1024)]
        public int MediumPictureHeight { get; private set; } = 256;

        [Range(1, 1024)]
        public int LargePictureWidth { get; private set; } = 512;

        [Range(1, 1024)]
        public int LargePictureHeight { get; private set; } = 512;
    }
}