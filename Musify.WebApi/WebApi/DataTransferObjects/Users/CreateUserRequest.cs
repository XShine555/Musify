namespace WebApi.DataTransferObjects.Users;

public record CreateUserRequest(
    long Id,
    string Name,
    string? FirstName,
    string? SecondName);
