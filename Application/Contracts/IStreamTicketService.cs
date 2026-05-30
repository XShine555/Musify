namespace Musify.Application.Contracts
{
    public interface IStreamTicketService
    {
        StreamTicket IssueTicket(Guid userId, string keyPrefix);
    }

    public record StreamTicket(string Token, int ExpiresInSeconds);
}
