using Musify.Application.Shared;

namespace Musify.Infrastructure.MassTransit.Arguments;

public record PublishPlayListPictureProcessingEventArguments(
    Guid PlayListId,
    string Bucket,
    string FinalPictureKey,
    ImageSize Small,
    ImageSize Medium,
    ImageSize Large);
