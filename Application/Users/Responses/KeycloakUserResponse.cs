namespace Musify.Application.Users.Responses
{
    public record KeycloakUserResponse(
        Guid Id,
        string Name,
        string FirstName,
        string SecondName);
}