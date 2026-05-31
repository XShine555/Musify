namespace Musify.Application.Contracts
{
    public interface IStreamTicketService
    {
        StreamTicket IssueTicket(long userId, string keyPrefix);
    }

    public record StreamTicket(string Token, int ExpiresInSeconds);
}
