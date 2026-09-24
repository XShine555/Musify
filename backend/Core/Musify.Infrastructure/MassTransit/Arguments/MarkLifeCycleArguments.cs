using Musify.Domain.ValueObjects;

namespace Musify.Infrastructure.MassTransit.Arguments
{
    internal record MarkLifeCycleArguments(Guid Id, LifeCycleStatus Status);
}
