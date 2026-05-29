using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Musify.Application.Users.Commands;
using System.Security.Claims;

namespace WebApi.Authentication;

public sealed class JwtBearerEventsHandler(IMediator mediator) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext tokenValidatedContext)
    {
        var principal = tokenValidatedContext.Principal!;

        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var guidId = Guid.Parse(id);

        var username = principal.FindFirstValue(ClaimTypes.Name)!;
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = principal.FindFirstValue(ClaimTypes.Surname);

        await mediator.Send(
            new CreateUserCommand(guidId, username, firstName, lastName),
            tokenValidatedContext.HttpContext.RequestAborted);
    }
}
