using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;

namespace Musify.Infrastructure.Persistence
{
    public class ProcessTrackingStore(IDatabase database)
        : IProcessTrackingStore
    {
        public async Task<Guid> GetOrCreateProcessAsync(
            string processName,
            Guid? correlationId,
            Guid? conversationId,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            ProcessExecution? existingProcess = null;

            if (correlationId.HasValue)
            {
                existingProcess = await database.ProcessExecutions
                    .OrderByDescending(process => process.StartedDateTime)
                    .FirstOrDefaultAsync(
                        process => process.CorrelationId == correlationId && process.ProcessName == processName,
                        cancellationToken);
            }

            if (existingProcess is not null)
            {
                return existingProcess.Id;
            }

            var newProcess = new ProcessExecution
            {
                ProcessName = processName,
                CorrelationId = correlationId,
                ConversationId = conversationId,
                MessageId = messageId,
                Status = ProcessExecutionStatus.Running,
                StartedDateTime = DateTime.UtcNow,
            };

            await database.ProcessExecutions.AddAsync(newProcess, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            return newProcess.Id;
        }

        public async Task<Guid> StartStepAsync(
            Guid processExecutionId,
            string stepName,
            ProcessStepComponentType componentType,
            int attempt,
            CancellationToken cancellationToken)
        {
            var step = new ProcessStepExecution
            {
                ProcessExecutionId = processExecutionId,
                StepName = stepName,
                ComponentType = componentType,
                Attempt = attempt,
                Status = ProcessExecutionStatus.Running,
                StartedDateTime = DateTime.UtcNow,
            };

            await database.ProcessStepExecutions.AddAsync(step, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            return step.Id;
        }

        public async Task CompleteStepAsync(Guid processExecutionId, Guid stepExecutionId, CancellationToken cancellationToken)
        {
            var step = await database.ProcessStepExecutions
                .SingleAsync(currentStep => currentStep.Id == stepExecutionId, cancellationToken);

            step.Status = ProcessExecutionStatus.Succeeded;
            step.FinishedDateTime = DateTime.UtcNow;

            var process = await database.ProcessExecutions
                .SingleAsync(currentProcess => currentProcess.Id == processExecutionId, cancellationToken);

            var hasRunningSteps = await database.ProcessStepExecutions
                .AnyAsync(
                    currentStep => currentStep.ProcessExecutionId == processExecutionId
                                   && currentStep.Status == ProcessExecutionStatus.Running,
                    cancellationToken);

            var hasFailedSteps = await database.ProcessStepExecutions
                .AnyAsync(
                    currentStep => currentStep.ProcessExecutionId == processExecutionId
                                   && currentStep.Status == ProcessExecutionStatus.Failed,
                    cancellationToken);

            if (!hasRunningSteps && !hasFailedSteps)
            {
                process.Status = ProcessExecutionStatus.Succeeded;
                process.FinishedDateTime = DateTime.UtcNow;
                process.ErrorMessage = string.Empty;
            }

            await database.SaveChangesAsync(cancellationToken);
        }

        public async Task FailStepAsync(Guid processExecutionId, Guid stepExecutionId, string errorMessage, CancellationToken cancellationToken)
        {
            var step = await database.ProcessStepExecutions
                .SingleAsync(currentStep => currentStep.Id == stepExecutionId, cancellationToken);

            step.Status = ProcessExecutionStatus.Failed;
            step.FinishedDateTime = DateTime.UtcNow;
            step.ErrorMessage = errorMessage;

            var process = await database.ProcessExecutions
                .SingleAsync(currentProcess => currentProcess.Id == processExecutionId, cancellationToken);

            process.Status = ProcessExecutionStatus.Failed;
            process.FinishedDateTime = DateTime.UtcNow;
            process.ErrorMessage = errorMessage;

            await database.SaveChangesAsync(cancellationToken);
        }
    }
}
