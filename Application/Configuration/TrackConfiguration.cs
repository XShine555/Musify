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

        public string SmallPicturesPath => Path.Combine(ParentFolder, SmallPicturesFolder);

        public string MediumPicturesPath => Path.Combine(ParentFolder, MediumPicturesFolder);

        public string LargePicturesPath => Path.Combine(ParentFolder, LargePicturesFolder);

        public string PresetSmallPicturePath => Path.Combine(ParentFolder, PresetSmallPicture);

        public string PresetMediumPicturePath => Path.Combine(ParentFolder, PresetMediumPicture);

        public string PresetLargePicturePath => Path.Combine(ParentFolder, PresetLargePicture);

        public string OriginalPicturesPath => Path.Combine(ParentFolder, OriginalPicturesFolder);

        public string OriginalAudiosPath => Path.Combine(ParentFolder, OriginalAudiosFolder);

        public string ProcessedAudiosPath => Path.Combine(ParentFolder, ProcessedAudioFolder);

        public string BuildOriginalPicturePath(string pictureName) => Path.Combine(OriginalPicturesPath, pictureName);

        public string BuildSmallPicturePath(string pictureName) => Path.Combine(SmallPicturesPath, pictureName);

        public string BuildMediumPicturePath(string pictureName) => Path.Combine(MediumPicturesPath, pictureName);

        public string BuildLargePicturePath(string pictureName) => Path.Combine(LargePicturesPath, pictureName);

        public string BuildOriginalAudioPath(string audioName) => Path.Combine(OriginalAudiosPath, audioName);

        public string BuildProcessedAudioPath(string audioName) => Path.Combine(ProcessedAudiosPath, audioName);
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