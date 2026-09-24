using System.ComponentModel.DataAnnotations;
using Musify.Application.Shared;

namespace Musify.Application.Configuration;

public class AlbumConfiguration
{
    public const string SectionName = "Album";

    [Required]
    public AlbumRoutes Routes { get; set; } = new AlbumRoutes();

    [Required]
    public AlbumPicturesSizes PicturesSizes { get; set; } = new AlbumPicturesSizes();
}

public class AlbumRoutes
{
    [Required]
    public string UploadsFolder { get; set; } = "uploads";

    [Required]
    public string ParentFolders { get; set; } = "Albums";

    [Required]
    public string OriginalPicturesFolder { get; set; } = "OriginalPictures";

    [Required]
    public string SmallPicturesFolder { get; set; } = "SmallPictures";

    [Required]
    public string MediumPicturesFolder { get; set; } = "MediumPictures";

    [Required]
    public string LargePicturesFolder { get; set; } = "LargePictures";

    public string SmallPicturesPath => StorageKey.Combine(ParentFolders, SmallPicturesFolder);

    public string MediumPicturesPath => StorageKey.Combine(ParentFolders, MediumPicturesFolder);

    public string LargePicturesPath => StorageKey.Combine(ParentFolders, LargePicturesFolder);

    public string OriginalPicturesPath => StorageKey.Combine(ParentFolders, OriginalPicturesFolder);

    public string BuildOriginalPicturePath(string pictureName) => StorageKey.Combine(ParentFolders, OriginalPicturesFolder, pictureName);

    public string BuildOriginalPicturePath(long userId, string pictureName) => StorageKey.Combine(UploadsFolder, userId.ToString(), ParentFolders, OriginalPicturesFolder, pictureName);

    public string BuildSmallPicturePath(string pictureName) => StorageKey.Combine(ParentFolders, SmallPicturesFolder, pictureName);

    public string BuildMediumPicturePath(string pictureName) => StorageKey.Combine(ParentFolders, MediumPicturesFolder, pictureName);

    public string BuildLargePicturePath(string pictureName) => StorageKey.Combine(ParentFolders, LargePicturesFolder, pictureName);

    public string BuildTempPicturePath(string tempRootPrefix, long userId, string objectName) =>
        StorageKey.Combine(tempRootPrefix, userId.ToString(), ParentFolders, objectName);
}

public class AlbumPicturesSizes
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
