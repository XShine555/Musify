using MassTransit;

namespace Musify.Infrastructure.MassTransit.Sagas
{
    public class AlbumProcessingState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = null!;

        public string? Bucket { get; set; }

        public string? PictureKey { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
