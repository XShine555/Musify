namespace Musify.Application.Shared;

public record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
}
