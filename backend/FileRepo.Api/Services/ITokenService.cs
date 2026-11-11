using System.Security.Claims;
using FileRepo.Api.Models;

namespace FileRepo.Api.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateTwoFactorToken(User user);
    ClaimsPrincipal? ValidateTwoFactorToken(string token);
}
