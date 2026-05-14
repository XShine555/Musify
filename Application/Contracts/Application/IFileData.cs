namespace Musify.Application.Contracts.Application
{
    public interface IFileData
    {
        Stream FileStream { get; }

        string FileName { get; }

        string FileType { get; }

        string ContentType { get; }
    }
}