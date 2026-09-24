namespace Musify.Application.Tracks;

public static class ListeningRules
{
    public const double CountedThresholdSeconds = 30;
    public const double CountedThresholdFraction = 0.5;
    public const double DurationToleranceSeconds = 2;
    public const double ClockToleranceSeconds = 5;
    public const int UncountedRetentionDays = 8;

    public static double CountedThreshold(double durationSeconds) =>
        Math.Min(CountedThresholdSeconds, durationSeconds * CountedThresholdFraction);
}
