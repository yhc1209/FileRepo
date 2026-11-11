using System.Security.Claims;
using System.Threading.Tasks;
using FileRepo.Api.Models.Auth;
using FileRepo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileRepo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("verify-2fa")]
    public async Task<ActionResult<AuthResponse>> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.VerifyTwoFactorAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}
