namespace FIAP.CloudGames.Usuario.API.Domain.Events;

public record UserCreatedEvent(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt
);

public record UserUpdatedEvent(
    Guid UserId,
    string Name,
    string Email,
    DateTime UpdatedAt
);

public record UserLoggedInEvent(
    Guid UserId,
    string Email,
    DateTime LoggedInAt
);
