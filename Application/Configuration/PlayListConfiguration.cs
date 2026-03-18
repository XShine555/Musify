using Microsoft.Extensions.Configuration;

namespace Musify.Application.Configuration
{
    public class PlayListConfiguration
    {
        public const string SectionName = "PlayListConfiguration";

        public PlayListRoutes Routes { get; set; } = new PlayListRoutes();

        public int SmallPictureWidth { get; set; } = 128;

        public int SmallPictureHeight { get; set; } = 128;

        public int MediumPictureWidth { get; private set; } = 256;

        public int MediumPictureHeight { get; private set; } = 256;

        public int LargePictureWidth { get; private set; } = 512;

        public int LargePictureHeight { get; private set; } = 512;

        public PlayListConfiguration Load(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName).Get<PlayListConfiguration>()
                ?? throw new InvalidOperationException($"{SectionName} configuration section not found.");

            Validate();

            return section;
        }

        void Validate()
        {
            EnsureValue(Routes.ParentFolder, $"{SectionName}:BucketName");
            EnsureValue(Routes.SmallPictures, $"{SectionName}:Routes:SmallPictures");
            EnsureValue(Routes.MediumPictures, $"{SectionName}:Routes:MediumPictures");
            EnsureValue(Routes.LargePictures, $"{SectionName}:Routes:LargePictures");

            EnsurePositiveValue(SmallPictureWidth, $"{SectionName}:SmallPictureWidth");
            EnsurePositiveValue(SmallPictureHeight, $"{SectionName}:SmallPictureHeight");
            EnsurePositiveValue(MediumPictureWidth, $"{SectionName}:MediumPictureWidth");
            EnsurePositiveValue(MediumPictureHeight, $"{SectionName}:MediumPictureHeight");
            EnsurePositiveValue(LargePictureWidth, $"{SectionName}:LargePictureWidth");
            EnsurePositiveValue(LargePictureHeight, $"{SectionName}:LargePictureHeight");
        }

        void EnsureValue(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"{name} configuration value not found.");
        }

        void EnsurePositiveValue(int value, string name)
        {
            if (value < 1)
                throw new InvalidOperationException($"{name} configuration value must be a positive integer.");
        }
    }

    public class PlayListRoutes
    {
        public string ParentFolder { get; set; } = "PlayLists";

        public string SmallPictures { get; set; } = "SmallPictures";

        public string MediumPictures { get; set; } = "MediumPictures";

        public string LargePictures { get; set; } = "LargePictures";

        public string DefaultSmallPicture { get; set; } = "DefaultSmallPicture.webp";

        public string DefaultMediumPicture { get; set; } = "DefaultMediumPicture.webp";

        public string DefaultLargePicture { get; set; } = "DefaultLargePicture.webp";
    }
}