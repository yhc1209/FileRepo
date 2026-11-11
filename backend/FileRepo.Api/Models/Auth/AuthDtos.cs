using System;
using System.ComponentModel.DataAnnotations;

namespace FileRepo.Api.Models.Auth;

public class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class VerifyTwoFactorRequest
{
    [Required]
    public string TwoFactorToken { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsTwoFactorEnabled { get; set; }
    public string TwoFactorType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AuthResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorToken { get; set; }
    public string? AccessToken { get; set; }
    public UserDto? User { get; set; }
}
