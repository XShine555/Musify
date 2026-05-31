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
        public string UploadsFolder { get; set; } = "uploads";

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

        public string SmallPicturesPath => CombineKey(ParentFolders, SmallPicturesFolder);

        public string MediumPicturesPath => CombineKey(ParentFolders, MediumPicturesFolder);

        public string LargePicturesPath => CombineKey(ParentFolders, LargePicturesFolder);

        public string PresetSmallPicturePath => CombineKey(ParentFolders, PresetSmallPicture);

        public string PresetMediumPicturePath => CombineKey(ParentFolders, PresetMediumPicture);

        public string PresetLargePicturePath => CombineKey(ParentFolders, PresetLargePicture);

        public string OriginalPicturesPath => CombineKey(ParentFolders, OriginalPicturesFolder);

        public string BuildOriginalPicturePath(string pictureName) => CombineKey(ParentFolders, OriginalPicturesFolder, pictureName);

        public string BuildOriginalPicturePath(long userId, string pictureName) => CombineKey(UploadsFolder, userId.ToString(), ParentFolders, OriginalPicturesFolder, pictureName);

        public string BuildSmallPicturePath(string pictureName) => CombineKey(ParentFolders, SmallPicturesFolder, pictureName);

        public string BuildMediumPicturePath(string pictureName) => CombineKey(ParentFolders, MediumPicturesFolder, pictureName);

        public string BuildLargePicturePath(string pictureName) => CombineKey(ParentFolders, LargePicturesFolder, pictureName);

        public string BuildTempPicturePath(string tempRootPrefix, long userId, string objectName) =>
            CombineKey(tempRootPrefix, userId.ToString(), ParentFolders, objectName);

        static string CombineKey(params string[] segments)
        {
            return string.Join('/', segments
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => s.Trim().Trim('/', '\\')));
        }
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