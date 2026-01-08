using System.ComponentModel.DataAnnotations;

namespace FIAP.CloudGames.Usuario.API.Application.DTOs;

public record CreateUserDto(
    [Required] [StringLength(100)] string Name,
    [Required] [EmailAddress] string Email,
    [Required] [MinLength(8)] string Password);

public record UpdateUserDto(
    [Required] [StringLength(100)] string Name,
    [Required] [EmailAddress] string Email);

public record UserResponseDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record LoginDto(
    [Required] [EmailAddress] string Email,
    [Required] string Password);

public record LoginResponseDto(
    string Token,
    UserResponseDto User);
