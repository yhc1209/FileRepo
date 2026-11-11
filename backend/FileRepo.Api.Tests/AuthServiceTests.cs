using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FileRepo.Api.Data;
using FileRepo.Api.Models;
using FileRepo.Api.Models.Auth;
using FileRepo.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FileRepo.Api.Tests;

public class AuthServiceTests
{
    private readonly DbContextOptions<FileDbContext> _dbOptions;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITwoFactorProvider _twoFactorProvider;

    public AuthServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<FileDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tokenService = Substitute.For<ITokenService>();
        _passwordHasher = new PasswordHasher<User>();
        _twoFactorProvider = Substitute.For<ITwoFactorProvider>();
    }

    private FileDbContext CreateDbContext() => new FileDbContext(_dbOptions);

    [Fact]
    public async Task RegisterAsync_ValidRequest_ShouldCreateUserAndReturnAccessToken()
    {
        // Arrange
        using var db = CreateDbContext();
        _tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("mock-access-token");
        var sut = new AuthService(db, _tokenService, _passwordHasher);

        var request = new RegisterRequest
        {
            Username = "john_doe",
            Email = "john@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("mock-access-token");
        result.RequiresTwoFactor.Should().BeFalse();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("john_doe");

        var savedUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "john_doe");
        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().NotBe("Password123!");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsernameOrEmail_ShouldFail()
    {
        // Arrange
        using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Username = "existing_user",
            Email = "existing@example.com",
            PasswordHash = "hash"
        });
        await db.SaveChangesAsync();

        var sut = new AuthService(db, _tokenService, _passwordHasher);

        var request = new RegisterRequest
        {
            Username = "existing_user",
            Email = "different@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("已被註冊");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentialsWithoutMfa_ShouldReturnAccessToken()
    {
        // Arrange
        using var db = CreateDbContext();
        var user = new User
        {
            Username = "alice",
            Email = "alice@example.com",
            IsTwoFactorEnabled = false
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "SecurePass123");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        _tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("alice-access-token");
        var sut = new AuthService(db, _tokenService, _passwordHasher);

        var request = new LoginRequest
        {
            UsernameOrEmail = "alice",
            Password = "SecurePass123"
        };

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.RequiresTwoFactor.Should().BeFalse();
        result.AccessToken.Should().Be("alice-access-token");
        result.TwoFactorToken.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldFail()
    {
        // Arrange
        using var db = CreateDbContext();
        var user = new User
        {
            Username = "bob",
            Email = "bob@example.com"
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "CorrectPassword");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new AuthService(db, _tokenService, _passwordHasher);

        var request = new LoginRequest
        {
            UsernameOrEmail = "bob",
            Password = "WrongPassword"
        };

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("帳號或密碼錯誤");
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_UserWithMfaEnabled_ShouldRequireTwoFactor_AndReturnTwoFactorToken()
    {
        // Arrange
        using var db = CreateDbContext();
        var user = new User
        {
            Username = "charlie_mfa",
            Email = "charlie@example.com",
            IsTwoFactorEnabled = true,
            TwoFactorType = TwoFactorType.Totp,
            TwoFactorSecret = "BASE32SECRETKEY"
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        _tokenService.GenerateTwoFactorToken(Arg.Is<User>(u => u.Id == user.Id)).Returns("intermediate-2fa-token");
        var sut = new AuthService(db, _tokenService, _passwordHasher);

        var request = new LoginRequest
        {
            UsernameOrEmail = "charlie_mfa",
            Password = "Password123!"
        };

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.RequiresTwoFactor.Should().BeTrue();
        result.TwoFactorToken.Should().Be("intermediate-2fa-token");
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task VerifyTwoFactorAsync_ValidCode_ShouldIssueAccessToken()
    {
        // Arrange
        using var db = CreateDbContext();
        var user = new User
        {
            Username = "dave_mfa",
            Email = "dave@example.com",
            IsTwoFactorEnabled = true,
            TwoFactorType = TwoFactorType.Totp,
            TwoFactorSecret = "MYSECRET"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("scope", "mfa_pending")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _tokenService.ValidateTwoFactorToken("valid-2fa-token").Returns(principal);
        _tokenService.GenerateAccessToken(Arg.Is<User>(u => u.Id == user.Id)).Returns("final-access-token");
        _twoFactorProvider.ValidateCode("MYSECRET", "654321").Returns(true);

        var sut = new AuthService(db, _tokenService, _passwordHasher, _twoFactorProvider);

        var request = new VerifyTwoFactorRequest
        {
            TwoFactorToken = "valid-2fa-token",
            Code = "654321"
        };

        // Act
        var result = await sut.VerifyTwoFactorAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().Be("final-access-token");
        result.RequiresTwoFactor.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyTwoFactorAsync_InvalidCode_ShouldFail()
    {
        // Arrange
        using var db = CreateDbContext();
        var user = new User
        {
            Username = "eve",
            Email = "eve@example.com",
            IsTwoFactorEnabled = true,
            TwoFactorSecret = "MYSECRET"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("scope", "mfa_pending")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        _tokenService.ValidateTwoFactorToken("valid-2fa-token").Returns(principal);
        _twoFactorProvider.ValidateCode("MYSECRET", "999999").Returns(false);

        var sut = new AuthService(db, _tokenService, _passwordHasher, _twoFactorProvider);

        var request = new VerifyTwoFactorRequest
        {
            TwoFactorToken = "valid-2fa-token",
            Code = "999999"
        };

        // Act
        var result = await sut.VerifyTwoFactorAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("驗證碼錯誤");
        result.AccessToken.Should().BeNull();
    }
}
