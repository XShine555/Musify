using Microsoft.Extensions.Configuration;

namespace Musify.Application.Configuration
{
    public class PlayListConfiguration
    {
        public const string SectionName = "PlayListConfiguration";

        public Routes Routes { get; set; } = new Routes();

        public PicturesSizes PicturesSizes { get; set; } = new PicturesSizes();

        public static PlayListConfiguration Load(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName).Get<PlayListConfiguration>()
                ?? throw new InvalidOperationException($"{SectionName} configuration section not found.");

            Validate(section);
            return section;
        }

        static void Validate(PlayListConfiguration configuration)
        {
            EnsureValue(configuration.Routes.OriginalPictures, $"{SectionName}:BucketName");
            EnsureValue(configuration.Routes.SmallPictures, $"{SectionName}:Routes:SmallPictures");
            EnsureValue(configuration.Routes.MediumPictures, $"{SectionName}:Routes:MediumPictures");
            EnsureValue(configuration.Routes.LargePictures, $"{SectionName}:Routes:LargePictures");
            EnsureValue(configuration.Routes.PresetSmallPicture, $"{SectionName}:Routes:SmallPictures");
            EnsureValue(configuration.Routes.PresetMediumPicture, $"{SectionName}:Routes:MediumPictures");
            EnsureValue(configuration.Routes.PresetLargePicture, $"{SectionName}:Routes:LargePictures");

            EnsurePositiveValue(configuration.PicturesSizes.SmallPictureWidth, $"{SectionName}:SmallPictureWidth");
            EnsurePositiveValue(configuration.PicturesSizes.SmallPictureHeight, $"{SectionName}:SmallPictureHeight");
            EnsurePositiveValue(configuration.PicturesSizes.MediumPictureWidth, $"{SectionName}:MediumPictureWidth");
            EnsurePositiveValue(configuration.PicturesSizes.MediumPictureHeight, $"{SectionName}:MediumPictureHeight");
            EnsurePositiveValue(configuration.PicturesSizes.LargePictureWidth, $"{SectionName}:LargePictureWidth");
            EnsurePositiveValue(configuration.PicturesSizes.LargePictureHeight, $"{SectionName}:LargePictureHeight");
        }

        static void EnsureValue(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"{name} configuration value not found.");
        }

        static void EnsurePositiveValue(int value, string name)
        {
            if (value < 1)
                throw new InvalidOperationException($"{name} configuration value must be a positive integer.");
        }
    }

    public class Routes
    {
        public string OriginalPictures { get; set; } = "OriginalPictures";

        public string SmallPictures { get; set; } = "SmallPictures";

        public string MediumPictures { get; set; } = "MediumPictures";

        public string LargePictures { get; set; } = "LargePictures";

        public string PresetSmallPicture { get; set; } = "PresetSmallPicture.webp";

        public string PresetMediumPicture { get; set; } = "PresettMediumPicture.webp";

        public string PresetLargePicture { get; set; } = "PresetLargePicture.webp";
    }

    public class PicturesSizes
    {
        public int SmallPictureWidth { get; set; } = 128;

        public int SmallPictureHeight { get; set; } = 128;

        public int MediumPictureWidth { get; private set; } = 256;

        public int MediumPictureHeight { get; private set; } = 256;

        public int LargePictureWidth { get; private set; } = 512;

        public int LargePictureHeight { get; private set; } = 512;
    }
}