using System.ComponentModel.DataAnnotations;
using Musify.Application.Shared;

namespace Musify.Application.Configuration;

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

    public string SmallPicturesPath => StorageKey.Combine(ParentFolder, SmallPicturesFolder);

    public string MediumPicturesPath => StorageKey.Combine(ParentFolder, MediumPicturesFolder);

    public string LargePicturesPath => StorageKey.Combine(ParentFolder, LargePicturesFolder);

    public string PresetSmallPicturePath => StorageKey.Combine(ParentFolder, PresetSmallPicture);

    public string PresetMediumPicturePath => StorageKey.Combine(ParentFolder, PresetMediumPicture);

    public string PresetLargePicturePath => StorageKey.Combine(ParentFolder, PresetLargePicture);

    public string OriginalPicturesPath => StorageKey.Combine(ParentFolder, OriginalPicturesFolder);

    public string OriginalAudiosPath => StorageKey.Combine(ParentFolder, OriginalAudiosFolder);

    public string ProcessedAudiosPath => StorageKey.Combine(ParentFolder, ProcessedAudioFolder);

    public string BuildOriginalPicturePath(string pictureName) => StorageKey.Combine(OriginalPicturesPath, pictureName);

    public string BuildOriginalPicturePath(long userId, string pictureName) => StorageKey.Combine(UploadsFolder, userId.ToString(), OriginalPicturesPath, pictureName);

    public string BuildSmallPicturePath(string pictureName) => StorageKey.Combine(SmallPicturesPath, pictureName);

    public string BuildMediumPicturePath(string pictureName) => StorageKey.Combine(MediumPicturesPath, pictureName);

    public string BuildLargePicturePath(string pictureName) => StorageKey.Combine(LargePicturesPath, pictureName);

    public string BuildOriginalAudioPath(string audioName) => StorageKey.Combine(OriginalAudiosPath, audioName);

    public string BuildOriginalAudioPath(long userId, string audioName) => StorageKey.Combine(UploadsFolder, userId.ToString(), OriginalAudiosPath, audioName);

    public string BuildProcessedAudioPath(string audioName) => StorageKey.Combine(ProcessedAudiosPath, audioName);

    public string BuildTempPicturePath(string tempRootPrefix, long userId, string objectName) =>
        StorageKey.Combine(tempRootPrefix, userId.ToString(), ParentFolder, objectName);

    public string BuildTempAudioPath(string tempRootPrefix, long userId, string objectName) =>
        StorageKey.Combine(tempRootPrefix, userId.ToString(), ParentFolder, objectName);
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
