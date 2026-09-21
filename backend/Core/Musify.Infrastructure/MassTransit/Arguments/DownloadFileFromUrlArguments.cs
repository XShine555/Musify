namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record DownloadFileFromUrlArguments(
        string SourceUrl,
        string DestinationFilePathVariable);
}
