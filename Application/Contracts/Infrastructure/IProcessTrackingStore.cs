using Musify.Domain.Entities;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IProcessTrackingStore
    {
        Task<Guid> GetOrCreateProcessAsync(
            string processName,
            Guid? correlationId,
            Guid? conversationId,
            Guid? messageId,
            CancellationToken cancellationToken);

        Task<Guid> StartStepAsync(
            Guid processExecutionId,
            string stepName,
            ProcessStepComponentType componentType,
            int attempt,
            CancellationToken cancellationToken);

        Task CompleteStepAsync(Guid processExecutionId, Guid stepExecutionId, CancellationToken cancellationToken);

        Task FailStepAsync(Guid processExecutionId, Guid stepExecutionId, string errorMessage, CancellationToken cancellationToken);
    }
}
