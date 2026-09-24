using System.Diagnostics.CodeAnalysis;

namespace Musify.Domain.ValueObjects;

#pragma warning disable CS8618
public class TrackAudio
{
    public string? OriginalName { get; set; }

    public string? FolderName { get; set; }

    public ProcessingStatus TranscodeStatus { get; set; } = ProcessingStatus.Pending;

    [MemberNotNullWhen(true, nameof(FolderName))]
    public bool IsProcessed => TranscodeStatus == ProcessingStatus.Completed;

    public bool IsFailed => TranscodeStatus == ProcessingStatus.Failed;

    public bool IsInProgress => TranscodeStatus is ProcessingStatus.Pending or ProcessingStatus.Processing;
}
