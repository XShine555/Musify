namespace Musify.Application.Contracts
{
    public interface IStreamTicketService
    {
        /// <param name="userId">Null for an anonymous listener — the ticket is issued without a
        /// <c>sub</c> claim.</param>
        /// <param name="keyPrefix">The storage key (prefix) the ticket authorizes reading from.</param>
        /// <param name="maxBytes">When set, the Streaming Gateway clamps the byte range it will serve
        /// for this ticket to <c>[0, maxBytes)</c> — used for the anonymous preview fragment.</param>
        StreamTicket IssueTicket(long? userId, string keyPrefix, long? maxBytes = null);
    }

    public record StreamTicket(string Token, int ExpiresInSeconds);
}
