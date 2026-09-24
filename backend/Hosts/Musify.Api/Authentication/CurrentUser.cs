using System.Reflection;
using System.Security.Claims;

namespace Musify.Api.Authentication;

public sealed class CurrentUser : IBindableFromHttpContext<CurrentUser>
{
    private CurrentUser(ClaimsPrincipal principal)
    {
        IsAuthenticated = principal.Identity?.IsAuthenticated ?? false;

        var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(rawId, out var id) && id != 0)
            Id = id;

        Username = principal.FindFirstValue(ClaimTypes.Name);
        FirstName = principal.FindFirstValue(ClaimTypes.GivenName);
        LastName = principal.FindFirstValue(ClaimTypes.Surname);
    }

    public bool IsAuthenticated { get; }

    public long? Id { get; }

    public string? Username { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public long RequiredId => Id ?? throw new InvalidOperationException("User Id claim is missing or invalid.");

    public static ValueTask<CurrentUser?> BindAsync(HttpContext context, ParameterInfo parameter) =>
        ValueTask.FromResult<CurrentUser?>(new CurrentUser(context.User));
}
