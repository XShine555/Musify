using FluentValidation;

namespace Musify.Api.Validators;

public static class Uploads
{
    public static readonly IReadOnlySet<string> PictureFileTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "jpg", "jpeg", "png", "webp" };

    public static readonly IReadOnlySet<string> PictureContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    public static readonly IReadOnlySet<string> AudioFileTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mp3", "wav", "flac", "ogg", "m4a", "aac" };

    public static readonly IReadOnlySet<string> AudioContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "audio/mpeg", "audio/mp3", "audio/wav", "audio/x-wav", "audio/wave", "audio/flac", "audio/x-flac",
            "audio/ogg", "audio/mp4", "audio/x-m4a", "audio/m4a", "audio/aac"
        };

    public static IRuleBuilderOptions<T, string> MustBeFileType<T>(
        this IRuleBuilder<T, string> rule, IReadOnlySet<string> allowed) =>
        rule
            .NotEmpty()
            .Length(1, 10)
            .Matches("^[A-Za-z0-9]+$").WithMessage("'{PropertyName}' must contain only letters and digits.")
            .Must(allowed.Contains).WithMessage("'{PropertyName}' must be one of: " + string.Join(", ", allowed) + ".");

    public static IRuleBuilderOptions<T, string> MustBeContentType<T>(
        this IRuleBuilder<T, string> rule, IReadOnlySet<string> allowed) =>
        rule
            .NotEmpty()
            .Must(allowed.Contains).WithMessage("'{PropertyName}' is not an allowed content type.");

    public static IRuleBuilderOptions<T, long?> MustBeValidUploadSize<T>(
        this IRuleBuilder<T, long?> rule, long maxUploadBytes) =>
        rule
            .Must(size => size is null || (size > 0 && size <= maxUploadBytes))
            .WithMessage($"'{{PropertyName}}' must be greater than 0 and at most {maxUploadBytes} bytes.");
}
