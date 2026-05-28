namespace WebApi.DataTransferObjects.Users;

public record CreateUserRequest(
    Guid Id,
    string Name,
    string? FirstName,
    string? SecondName);
