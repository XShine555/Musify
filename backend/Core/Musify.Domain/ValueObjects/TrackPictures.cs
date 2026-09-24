using System.Diagnostics.CodeAnalysis;

namespace Musify.Domain.ValueObjects;

public class TrackPictures
{
    public string? OriginalName { get; set; }

    public string? SmallName { get; set; }

    public string? MediumName { get; set; }

    public string? LargeName { get; set; }

    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;

    [MemberNotNullWhen(true, nameof(SmallName))]
    [MemberNotNullWhen(true, nameof(MediumName))]
    [MemberNotNullWhen(true, nameof(LargeName))]
    public bool IsProcessed => ProcessingStatus == ProcessingStatus.Completed;
}
