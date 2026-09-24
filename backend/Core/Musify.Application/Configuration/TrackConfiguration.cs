using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Musify.Application.Shared;

namespace Musify.Application.Configuration
{
    public class TrackConfiguration : IConfigurationOptions, IPictureOwnerConfiguration
    {
        public static string SectionName => "Track";

        [Required]
        public TrackRoutes Routes { get; set; } = new();

        [Required]
        public PictureSizes PicturesSizes { get; set; } = new();

        PictureRoutes IPictureOwnerConfiguration.Routes => Routes;
    }

    public class TrackRoutes : PictureRoutes
    {
        public TrackRoutes()
        {
            ParentFolder = "Tracks";
        }

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
        public string ProcessedAudiosFolder { get; set; } = "ProcessedAudios";

        public string PresetSmallPicturePath => StorageKey.Combine(ParentFolder, PresetSmallPicture);

        public string PresetMediumPicturePath => StorageKey.Combine(ParentFolder, PresetMediumPicture);

        public string PresetLargePicturePath => StorageKey.Combine(ParentFolder, PresetLargePicture);

        public string BuildOriginalAudioPath(long userId, string audioName) =>
            StorageKey.Combine(UploadsFolder, userId.ToString(CultureInfo.InvariantCulture), ParentFolder, OriginalAudiosFolder, audioName);

        public string BuildProcessedAudioPath(string folderName) =>
            StorageKey.Combine(ParentFolder, ProcessedAudiosFolder, folderName);
    }
}
