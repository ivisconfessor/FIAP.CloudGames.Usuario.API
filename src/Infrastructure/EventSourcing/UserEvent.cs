namespace FIAP.CloudGames.Usuario.API.Infrastructure.EventSourcing;

public abstract class UserEvent
{
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
    public string UserId { get; set; }

    protected UserEvent(Guid aggregateId, string eventType, string userId)
    {
        EventId = Guid.NewGuid();
        AggregateId = aggregateId;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
        UserId = userId;
    }
}

public class UserCreatedEvent : UserEvent
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }

    public UserCreatedEvent(Guid userId, string name, string email, string role) 
        : base(userId, nameof(UserCreatedEvent), userId.ToString())
    {
        Name = name;
        Email = email;
        Role = role;
    }
}

public class UserUpdatedEvent : UserEvent
{
    public string Name { get; set; }
    public string Email { get; set; }

    public UserUpdatedEvent(Guid userId, string name, string email) 
        : base(userId, nameof(UserUpdatedEvent), userId.ToString())
    {
        Name = name;
        Email = email;
    }
}

public class UserLoggedInEvent : UserEvent
{
    public string Email { get; set; }
    public bool Success { get; set; }

    public UserLoggedInEvent(Guid userId, string email, bool success) 
        : base(userId, nameof(UserLoggedInEvent), userId.ToString())
    {
        Email = email;
        Success = success;
    }
}
