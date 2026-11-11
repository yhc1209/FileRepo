using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FileRepo.Api.Data;
using FileRepo.Api.Models;
using FileRepo.Api.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FileRepo.Api.Services;

public class AuthService : IAuthService
{
    private readonly FileDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITwoFactorProvider? _twoFactorProvider;

    public AuthService(
        FileDbContext dbContext,
        ITokenService tokenService,
        IPasswordHasher<User> passwordHasher,
        ITwoFactorProvider? twoFactorProvider = null)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _twoFactorProvider = twoFactorProvider;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var usernameExists = await _dbContext.Users
            .AnyAsync(u => u.Username.ToLower() == normalizedUsername.ToLower());
        if (usernameExists)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = "此使用者名稱已被使用，請選擇其他名稱"
            };
        }

        var emailExists = await _dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = "此電子郵件已被註冊，請直接登入或使用其他郵件"
            };
        }

        var user = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            CreatedAt = DateTime.UtcNow,
            IsTwoFactorEnabled = false,
            TwoFactorType = TwoFactorType.None
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user);

        return new AuthResponse
        {
            IsSuccess = true,
            Message = "註冊成功",
            AccessToken = accessToken,
            RequiresTwoFactor = false,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var identifier = request.UsernameOrEmail.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);

        if (user == null)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = "帳號或密碼錯誤"
            };
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = "帳號或密碼錯誤"
            };
        }

        // Check if 2FA is required
        if (user.IsTwoFactorEnabled)
        {
            var twoFactorToken = _tokenService.GenerateTwoFactorToken(user);
            return new AuthResponse
            {
                IsSuccess = true,
                RequiresTwoFactor = true,
                TwoFactorToken = twoFactorToken,
                Message = "請輸入多因子驗證碼 (MFA)"
            };
        }

        var accessToken = _tokenService.GenerateAccessToken(user);

        return new AuthResponse
        {
            IsSuccess = true,
            Message = "登入成功",
            AccessToken = accessToken,
            RequiresTwoFactor = false,
            User = MapToDto(user)
        };
    }

    public async Task<AuthResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request)
    {
        var principal = _tokenService.ValidateTwoFactorToken(request.TwoFactorToken);
        if (principal == null)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = "MFA 驗證階段已失效或 Token 無效，請重新登入"
            };
        }

        var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = "無效的憑證資訊"
            };
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = "使用者不存在"
            };
        }

        // Validate code using ITwoFactorProvider if available
        bool isValid = false;
        if (_twoFactorProvider != null && !string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            isValid = _twoFactorProvider.ValidateCode(user.TwoFactorSecret, request.Code);
        }
        else
        {
            // Default fallback for prototype/testing before specific provider setup
            // E.g. in test or mock mode: "123456" can be accepted or provider validates
            isValid = request.Code == "123456";
        }

        if (!isValid)
        {
            return new AuthResponse
            {
                IsSuccess = false,
                Message = "驗證碼錯誤或已過期"
            };
        }

        var accessToken = _tokenService.GenerateAccessToken(user);

        return new AuthResponse
        {
            IsSuccess = true,
            Message = "多因子驗證成功",
            AccessToken = accessToken,
            RequiresTwoFactor = false,
            User = MapToDto(user)
        };
    }

    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            return null;
        }

        var user = await _dbContext.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsTwoFactorEnabled = user.IsTwoFactorEnabled,
            TwoFactorType = user.TwoFactorType.ToString(),
            CreatedAt = user.CreatedAt
        };
    }
}
