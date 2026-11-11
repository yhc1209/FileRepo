using System;

namespace FileRepo.Api.Models;

public enum TwoFactorType
{
    None = 0,
    Totp = 1,
    Email = 2,
    Sms = 3
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // MFA extensible fields
    public bool IsTwoFactorEnabled { get; set; } = false;
    public TwoFactorType TwoFactorType { get; set; } = TwoFactorType.None;
    public string? TwoFactorSecret { get; set; }
}
