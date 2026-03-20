namespace Musify.Application.Contracts.Application
{
    public interface IEvent
    {
        Guid JobId { get; }
    }
}