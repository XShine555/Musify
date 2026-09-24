namespace Musify.Infrastructure.MassTransit.Logs;

public record ConsumeUploadIntentsLog(
    Guid[] IntentIds);
