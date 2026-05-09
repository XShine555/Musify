using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class PlayListConfiguration
    {
        public const string SectionName = "PlayList";

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
        public string OriginalPicturesFolder { get; set; } = "OriginalPictures";

        [Required]
        public string SmallPicturesFolder { get; set; } = "SmallPictures";

        [Required]
        public string MediumPicturesFolder { get; set; } = "MediumPictures";

        [Required]
        public string LargePicturesFolder { get; set; } = "LargePictures";

        [Required]
        public string PresetOriginalPicture { get; set; } = "PresetOriginalPicture.webp";

        [Required]
        public string PresetSmallPicture { get; set; } = "PresetSmallPicture.webp";

        [Required]
        public string PresetMediumPicture { get; set; } = "PresetMediumPicture.webp";

        [Required]
        public string PresetLargePicture { get; set; } = "PresetLargePicture.webp";

        public string SmallPicturesPath => Path.Combine(ParentFolders, SmallPicturesFolder);

        public string MediumPicturesPath => Path.Combine(ParentFolders, MediumPicturesFolder);

        public string LargePicturesPath => Path.Combine(ParentFolders, LargePicturesFolder);

        public string PresetSmallPicturePath => Path.Combine(ParentFolders, PresetSmallPicture);

        public string PresetMediumPicturePath => Path.Combine(ParentFolders, PresetMediumPicture);

        public string PresetLargePicturePath => Path.Combine(ParentFolders, PresetLargePicture);

        public string OriginalPicturesPath => Path.Combine(ParentFolders, OriginalPicturesFolder);

        public string BuildOriginalPicturePath(string pictureName) => Path.Combine(ParentFolders, OriginalPicturesFolder, pictureName);

        public string BuildSmallPicturePath(string pictureName) => Path.Combine(ParentFolders, SmallPicturesFolder, pictureName);

        public string BuildMediumPicturePath(string pictureName) => Path.Combine(ParentFolders, MediumPicturesFolder, pictureName);

        public string BuildLargePicturePath(string pictureName) => Path.Combine(ParentFolders, LargePicturesFolder, pictureName);
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