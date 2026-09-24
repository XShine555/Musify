using Musify.Domain.ValueObjects;

namespace Musify.Domain.Abstractions
{
    public interface IHasLifeCycle
    {
        public Guid Id { get; }

        public LifeCycleStatus LifeCycleStatus { get; set; }
    }
}
