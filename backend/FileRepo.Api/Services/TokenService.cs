using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FileRepo.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FileRepo.Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _twoFactorTokenExpirationMinutes;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        var secret = _configuration["Jwt:Secret"] ?? "Default_FileRepo_Super_Secret_Key_At_Least_32_Bytes!";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _issuer = _configuration["Jwt:Issuer"] ?? "FileRepo.Api";
        _audience = _configuration["Jwt:Audience"] ?? "FileRepo.Client";
        _accessTokenExpirationMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpirationMinutes"], out var accessExp) ? accessExp : 60;
        _twoFactorTokenExpirationMinutes = int.TryParse(_configuration["Jwt:TwoFactorTokenExpirationMinutes"], out var twoFactorExp) ? twoFactorExp : 5;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("scope", "api_access")
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateTwoFactorToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("scope", "mfa_pending"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_twoFactorTokenExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateTwoFactorToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                var scopeClaim = principal.FindFirst("scope")?.Value;
                if (scopeClaim == "mfa_pending")
                {
                    return principal;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
