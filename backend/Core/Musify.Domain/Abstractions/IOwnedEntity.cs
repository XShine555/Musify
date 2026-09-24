namespace Musify.Domain.Abstractions
{
    public interface IOwnedEntity
    {
        public long OwnerUserId { get; }
    }
}
