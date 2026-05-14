using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class TrackConfiguration
    {
        public const string SectionName = "Track";

        [Required]
        public TrackRoutes Routes { get; set; } = new TrackRoutes();

        [Required]
        public TrackPicturesSizes PicturesSizes { get; set; } = new TrackPicturesSizes();
    }

    public class TrackRoutes
    {
        [Required]
        public string UploadsFolder { get; set; } = "uploads";

        [Required]
        public string ParentFolder { get; set; } = "Tracks";

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

        [Required]
        public string OriginalAudiosFolder { get; set; } = "OriginalAudios";

        [Required]
        public string ProcessedAudioFolder { get; set; } = "ProcessedAudios";

        public string SmallPicturesPath => CombineKey(ParentFolder, SmallPicturesFolder);

        public string MediumPicturesPath => CombineKey(ParentFolder, MediumPicturesFolder);

        public string LargePicturesPath => CombineKey(ParentFolder, LargePicturesFolder);

        public string PresetSmallPicturePath => CombineKey(ParentFolder, PresetSmallPicture);

        public string PresetMediumPicturePath => CombineKey(ParentFolder, PresetMediumPicture);

        public string PresetLargePicturePath => CombineKey(ParentFolder, PresetLargePicture);

        public string OriginalPicturesPath => CombineKey(ParentFolder, OriginalPicturesFolder);

        public string OriginalAudiosPath => CombineKey(ParentFolder, OriginalAudiosFolder);

        public string ProcessedAudiosPath => CombineKey(ParentFolder, ProcessedAudioFolder);

        public string BuildOriginalPicturePath(string pictureName) => CombineKey(OriginalPicturesPath, pictureName);

        public string BuildOriginalPicturePath(Guid userId, string pictureName) => CombineKey(UploadsFolder, userId.ToString(), OriginalPicturesPath, pictureName);

        public string BuildSmallPicturePath(string pictureName) => CombineKey(SmallPicturesPath, pictureName);

        public string BuildMediumPicturePath(string pictureName) => CombineKey(MediumPicturesPath, pictureName);

        public string BuildLargePicturePath(string pictureName) => CombineKey(LargePicturesPath, pictureName);

        public string BuildOriginalAudioPath(string audioName) => CombineKey(OriginalAudiosPath, audioName);

        public string BuildOriginalAudioPath(Guid userId, string audioName) => CombineKey(UploadsFolder, userId.ToString(), OriginalAudiosPath, audioName);

        public string BuildProcessedAudioPath(string audioName) => CombineKey(ProcessedAudiosPath, audioName);

        // Temp paths used in Phase 2 for pre-signed upload keys (moved to final on consumption)
        public string BuildTempPicturePath(string tempRootPrefix, Guid userId, string objectName) =>
            CombineKey(tempRootPrefix, userId.ToString(), ParentFolder, objectName);

        public string BuildTempAudioPath(string tempRootPrefix, Guid userId, string objectName) =>
            CombineKey(tempRootPrefix, userId.ToString(), ParentFolder, objectName);

        static string CombineKey(params string[] segments)
        {
            return string.Join('/', segments
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => s.Trim().Trim('/', '\\')));
        }
    }

    public class TrackPicturesSizes
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