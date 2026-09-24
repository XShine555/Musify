using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Musify.Application.Users;

namespace Musify.Api.Authentication;

public sealed class JwtBearerEventsHandler(
    IMediator mediator,
    IMemoryCache cache,
    IHttpClientFactory httpClientFactory,
    AuthenticationConfiguration authenticationConfiguration,
    ILogger<JwtBearerEventsHandler> logger) : JwtBearerEvents
{
    private static readonly TimeSpan UserSyncCacheTtl = TimeSpan.FromMinutes(15);

    private sealed record UserInfoResponse(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("preferred_username")] string? PreferredUsername,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("given_name")] string? GivenName,
        [property: JsonPropertyName("family_name")] string? FamilyName,
        [property: JsonPropertyName("picture")] string? Picture);

    public override async Task TokenValidated(TokenValidatedContext tokenValidatedContext)
    {
        var principal = tokenValidatedContext.Principal!;

        var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(rawId, out var userId))
        {
            logger.LogWarning("Token subject '{Subject}' is not a numeric id; skipping user provisioning", rawId);
            return;
        }

        var cacheKey = $"user-synced:{userId}";
        if (cache.TryGetValue(cacheKey, out _))
            return;

        var userInfo = await FetchUserInfoAsync(tokenValidatedContext, tokenValidatedContext.HttpContext.RequestAborted);

        var username = userInfo?.Name
            ?? userInfo?.PreferredUsername
            ?? principal.FindFirstValue("name")
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("preferred_username")
            ?? userInfo?.Email
            ?? principal.FindFirstValue("email")
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? rawId!;

        var firstName = userInfo?.GivenName
            ?? principal.FindFirstValue("given_name")
            ?? principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = userInfo?.FamilyName
            ?? principal.FindFirstValue("family_name")
            ?? principal.FindFirstValue(ClaimTypes.Surname);
        var profilePictureUrl = userInfo?.Picture ?? principal.FindFirstValue("picture");

        try
        {
            await mediator.Send(
                new SyncUserCommand(userId, username, firstName, lastName, profilePictureUrl),
                tokenValidatedContext.HttpContext.RequestAborted);

            cache.Set(cacheKey, true, UserSyncCacheTtl);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to sync user {UserId} during token validation", userId);
        }
    }

    private async Task<UserInfoResponse?> FetchUserInfoAsync(
        TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken)
    {
        var authorizationHeader = tokenValidatedContext.HttpContext.Request.Headers.Authorization.ToString();
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var accessToken = authorizationHeader["Bearer ".Length..].Trim();
        if (accessToken.Length == 0)
            return null;

        try
        {
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{authenticationConfiguration.IssuerAddress.TrimEnd('/')}/oidc/v1/userinfo")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            };

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Userinfo request returned {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserInfoResponse>(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to fetch userinfo from identity provider");
            return null;
        }
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        logger.LogWarning(context.Exception, "JWT authentication failed");
        return Task.CompletedTask;
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        if (context.AuthenticateFailure != null)
            logger.LogWarning("JWT challenge: {Error} - {Description}", context.Error, context.ErrorDescription);
        return Task.CompletedTask;
    }
}
