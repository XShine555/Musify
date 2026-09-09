using System.ComponentModel.DataAnnotations;

namespace Musify.StreamingGateway.Configuration;

public sealed class StreamTicketValidationOptions
{
    public const string SectionName = "StreamTicket";

    [Required]
    public required string PublicKeyPath { get; set; }

    [Required]
    public string Audience { get; set; } = "media-gateway";

    [Required]
    public string Issuer { get; set; } = "musify-webapi";

    public string QueryParameterName { get; set; } = "t";

    public string HeaderName { get; set; } = "X-Stream-Ticket";

    public string MediaPathPrefix { get; set; } = "/media";
}
