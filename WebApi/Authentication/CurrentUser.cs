using System.Reflection;
using System.Security.Claims;

namespace WebApi.Authentication;

public sealed class CurrentUser : IBindableFromHttpContext<CurrentUser>
{
    private CurrentUser(ClaimsPrincipal principal)
    {
        Principal = principal;
        var isAuthenticated = principal.Identity?.IsAuthenticated;
        IsAuthenticated = isAuthenticated.HasValue && isAuthenticated.Value;

        var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        Id = Guid.TryParse(rawId, out var id)? id : null;

        Username = principal.FindFirstValue(ClaimTypes.Name);
        FirstName = principal.FindFirstValue(ClaimTypes.GivenName);
        LastName = principal.FindFirstValue(ClaimTypes.Surname);
    }

    public ClaimsPrincipal Principal { get; }

    public bool IsAuthenticated { get; }

    public Guid? Id { get; }

    public string? Username { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public Guid RequiredId => Id.HasValue? Id.Value : throw new InvalidOperationException("User ID is required but not present.");

    public bool HasClaim(string type) => Principal.HasClaim(c => c.Type == type);

    public string? FindClaim(string type) => Principal.FindFirstValue(type);

    public static ValueTask<CurrentUser?> BindAsync(HttpContext context, ParameterInfo parameter) =>
        ValueTask.FromResult<CurrentUser?>(new CurrentUser(context.User));
}