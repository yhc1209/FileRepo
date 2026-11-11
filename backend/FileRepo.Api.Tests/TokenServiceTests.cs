using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FileRepo.Api.Models;
using FileRepo.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FileRepo.Api.Tests;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "Test_Super_Secret_Key_For_Unit_Testing_32_Bytes_Long!"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:AccessTokenExpirationMinutes", "30"},
            {"Jwt:TwoFactorTokenExpirationMinutes", "5"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeCorrectClaimsAndIssuer()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var tokenString = _tokenService.GenerateAccessToken(user);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "api_access");
    }

    [Fact]
    public void GenerateTwoFactorToken_ShouldHaveMfaPendingScope_AndCanBeValidated()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "mfauser",
            Email = "mfa@example.com"
        };

        // Act
        var tokenString = _tokenService.GenerateTwoFactorToken(user);
        var principal = _tokenService.ValidateTwoFactorToken(tokenString);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();
        principal.Should().NotBeNull();
        principal!.FindFirst("scope")?.Value.Should().Be("mfa_pending");
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void ValidateTwoFactorToken_WithAccessToken_ShouldReturnNull_DueToScopeMismatch()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };
        var accessToken = _tokenService.GenerateAccessToken(user);

        // Act - Attempting to use AccessToken as TwoFactorToken
        var principal = _tokenService.ValidateTwoFactorToken(accessToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateTwoFactorToken_WithInvalidSignature_ShouldReturnNull()
    {
        // Act
        var principal = _tokenService.ValidateTwoFactorToken("invalid.token.structure");

        // Assert
        principal.Should().BeNull();
    }
}
