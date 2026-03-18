namespace Musify.Application.Configuration
{
    public class PlayListConfiguration
    {
        public PlayListRoutes Routes { get; set; } = new PlayListRoutes();

        public int SmallPictureWidth { get; set; } = 128;

        public int SmallPictureHeight { get; set; } = 128;

        public int MediumPictureWidth { get; private set; } = 256;

        public int MediumPictureHeight { get; private set; } = 256;

        public int LargePictureWidth { get; private set; } = 512;

        public int LargePictureHeight { get; private set; } = 512;
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