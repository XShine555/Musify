namespace Musify.Infrastructure.MassTransit.Logs;

public record DownloadFileFromUrlLog(
    string SourceUrl,
    string DestinationFilePath);
