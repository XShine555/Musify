using MassTransit;

namespace Musify.Infrastructure.MassTransit.Sagas;

public class TrackProcessingState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = null!;

    public bool PictureProcessed { get; set; }

    public bool AudioProcessed { get; set; }

    public string? Bucket { get; set; }

    public string? PictureKey { get; set; }

    public string? AudioKey { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
