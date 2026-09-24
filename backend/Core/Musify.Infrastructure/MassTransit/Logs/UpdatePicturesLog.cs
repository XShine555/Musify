namespace Musify.Infrastructure.MassTransit.Logs
{
    public record UpdatePicturesLog(
        Guid SubjectId,
        string? PreviousOriginalName,
        string? PreviousSmallName,
        string? PreviousMediumName,
        string? PreviousLargeName);
}
