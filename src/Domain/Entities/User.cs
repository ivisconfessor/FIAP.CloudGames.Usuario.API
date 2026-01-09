using System.ComponentModel.DataAnnotations;

namespace FIAP.CloudGames.Usuario.API.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; private set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; private set; }
    
    [Required]
    public string PasswordHash { get; private set; }
    
    public UserRole Role { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private User() 
    {
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string name, string email, string password, UserRole role = UserRole.User)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string email)
    {
        Name = name;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool ValidatePassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }
}

public enum UserRole
{
    User,
    Admin
}
