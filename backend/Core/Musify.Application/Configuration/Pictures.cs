using System.ComponentModel.DataAnnotations;
using Musify.Application.Shared;

namespace Musify.Application.Configuration;

public enum PictureSize
{
    Small,
    Medium,
    Large
}

public class PictureRoutes
{
    [Required]
    public string UploadsFolder { get; set; } = "uploads";

    [Required]
    public string ParentFolder { get; set; } = string.Empty;

    [Required]
    public string OriginalPicturesFolder { get; set; } = "OriginalPictures";

    [Required]
    public string SmallPicturesFolder { get; set; } = "SmallPictures";

    [Required]
    public string MediumPicturesFolder { get; set; } = "MediumPictures";

    [Required]
    public string LargePicturesFolder { get; set; } = "LargePictures";

    public string FolderPath(PictureSize size) => StorageKey.Combine(ParentFolder, size switch
    {
        PictureSize.Small => SmallPicturesFolder,
        PictureSize.Medium => MediumPicturesFolder,
        PictureSize.Large => LargePicturesFolder,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
    });

    public string BuildPicturePath(PictureSize size, string pictureName) => StorageKey.Combine(FolderPath(size), pictureName);

    public string BuildOriginalPicturePath(long userId, string pictureName) =>
        StorageKey.Combine(UploadsFolder, userId.ToString(), ParentFolder, OriginalPicturesFolder, pictureName);

    public string BuildTempPath(string tempRootPrefix, long userId, string objectName) =>
        StorageKey.Combine(tempRootPrefix, userId.ToString(), ParentFolder, objectName);
}

public class PictureSizes
{
    [Range(1, 1024)]
    public int SmallWidth { get; set; } = 128;

    [Range(1, 1024)]
    public int SmallHeight { get; set; } = 128;

    [Range(1, 1024)]
    public int MediumWidth { get; set; } = 256;

    [Range(1, 1024)]
    public int MediumHeight { get; set; } = 256;

    [Range(1, 1024)]
    public int LargeWidth { get; set; } = 512;

    [Range(1, 1024)]
    public int LargeHeight { get; set; } = 512;

    public ImageSizes ToImageSizes(PictureRoutes routes) => new(
        new ImageSize(routes.FolderPath(PictureSize.Small), SmallWidth, SmallHeight),
        new ImageSize(routes.FolderPath(PictureSize.Medium), MediumWidth, MediumHeight),
        new ImageSize(routes.FolderPath(PictureSize.Large), LargeWidth, LargeHeight));
}

public interface IPictureOwnerConfiguration
{
    public PictureRoutes Routes { get; }

    public PictureSizes PicturesSizes { get; }
}

public static class PictureSizeParser
{
    /// <summary>Parses the <c>size</c> query value; anything unknown means <see cref="PictureSize.Medium"/>.</summary>
    public static PictureSize Parse(string? value) =>
        Enum.TryParse<PictureSize>(value, ignoreCase: true, out var size) && Enum.IsDefined(size)
            ? size
            : PictureSize.Medium;
}
