namespace Musify.Application.Users.Responses
{
    public record ListeningStatsResponse(int TracksThisWeek, int SecondsThisWeek, int StreakDays);
}
