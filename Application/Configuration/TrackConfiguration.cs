using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class TrackConfiguration
    {
        public const string SectionName = "TrackConfiguration";

        [Required]
        public TrackRoutes Routes { get; set; } = new TrackRoutes();

        [Required]
        public TrackPicturesSizes PicturesSizes { get; set; } = new TrackPicturesSizes();
    }

    public class TrackRoutes
    {
        [Required]
        public string ParentFolder { get; set; } = "Tracks";

        [Required]
        public string OriginalPictures { get; set; } = "OriginalTracks";

        [Required]
        public string SmallPictures { get; set; } = "SmallTracks";

        [Required]
        public string MediumPictures { get; set; } = "MediumTracks";

        [Required]
        public string LargePictures { get; set; } = "LargeTracks";

        [Required]
        public string PresetSmallPicture { get; set; } = "PresetSmallPicture.webp";

        [Required]
        public string PresetMediumPicture { get; set; } = "PresetMediumPicture.webp";

        [Required]
        public string PresetLargePicture { get; set; } = "PresetLargePicture.webp";
    }

    public class TrackPicturesSizes
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