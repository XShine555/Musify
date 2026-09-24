namespace Musify.Domain.Abstractions;

public interface IOwnedEntity
{
    public Guid Id { get; }

    public long OwnerUserId { get; }
}
