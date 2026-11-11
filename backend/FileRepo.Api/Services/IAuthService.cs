using System.Threading.Tasks;
using FileRepo.Api.Models.Auth;

namespace FileRepo.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request);
    Task<UserDto?> GetCurrentUserAsync(string userId);
}
