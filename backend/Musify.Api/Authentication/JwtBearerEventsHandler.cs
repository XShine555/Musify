using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Musify.Application.Users;
using System.Security.Claims;

namespace Musify.Api.Authentication;

public sealed class JwtBearerEventsHandler(IMediator mediator, ILogger<JwtBearerEventsHandler> logger) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext tokenValidatedContext)
    {
        var principal = tokenValidatedContext.Principal!;

        var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(rawId, out var userId))
        {
            logger.LogWarning("Token subject '{Subject}' is not a numeric id; skipping user provisioning", rawId);
            return;
        }

        var username = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("preferred_username")
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? rawId!;

        var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = principal.FindFirstValue(ClaimTypes.Surname);
        var profilePictureUrl = principal.FindFirstValue("picture");

        try
        {
            await mediator.Send(
                new SyncUserCommand(userId, username, firstName, lastName, profilePictureUrl),
                tokenValidatedContext.HttpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to sync user {UserId} during token validation", userId);
        }
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        logger.LogWarning(context.Exception, "JWT authentication failed");
        return Task.CompletedTask;
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        if (context.AuthenticateFailure is not null)
            logger.LogWarning("JWT challenge: {Error} - {Description}", context.Error, context.ErrorDescription);
        return Task.CompletedTask;
    }
}
