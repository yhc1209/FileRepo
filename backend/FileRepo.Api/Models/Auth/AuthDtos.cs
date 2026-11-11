using System;
using System.ComponentModel.DataAnnotations;

namespace FileRepo.Api.Models.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "請輸入使用者名稱")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "使用者名稱長度需介於 3 到 50 個字元")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "使用者名稱僅能包含英數字、底線與連字號")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入電子郵件")]
    [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度至少需 6 個字元")]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "請輸入使用者名稱或電子郵件")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    public string Password { get; set; } = string.Empty;
}

public class VerifyTwoFactorRequest
{
    [Required(ErrorMessage = "缺少兩階段驗證憑證")]
    public string TwoFactorToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 6 位數驗證碼")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "驗證碼必須為 6 碼數字")]
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
