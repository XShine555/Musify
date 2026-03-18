namespace Musify.Application.Users.Contracts
{
    public record KeycloakUserResponse(
        Guid Id,
        string Name,
        string FirstName,
        string SecondName);
}