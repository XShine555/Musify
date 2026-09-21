namespace Musify.Application.Contracts
{
    public interface IStreamTicketService
    {

        StreamTicket IssueTicket(long? userId, string keyPrefix, long? maxBytes = null);
    }

    public record StreamTicket(string Token, int ExpiresInSeconds);
}
