using System.Threading.Tasks;
using FileRepo.Api.Models;

namespace FileRepo.Api.Services;

public interface ITwoFactorProvider
{
    TwoFactorType SupportedType { get; }
    string GenerateSecret();
    string GenerateQrCodeUri(string username, string secret, string issuer = "FileRepo");
    bool ValidateCode(string secret, string code);
}
