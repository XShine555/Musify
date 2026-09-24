namespace Musify.Infrastructure.MassTransit.Arguments;

public record ConsumeUploadIntentsArguments(
    Guid[] IntentIds);
