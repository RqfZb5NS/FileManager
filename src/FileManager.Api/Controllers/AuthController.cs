using FileManager.Api.Dtos;
using FileManager.Application.Services;
using Microsoft.AspNetCore.Mvc;
using FileManager.Core.Entities;
using Microsoft.Extensions.Logging;
using FileManager.Core.Interfaces.Services;

namespace FileManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register(UserRegisterDto dto)
    {
        _logger.LogInformation(
            "Starting registration. Username: {Username}, Email: {Email}",
            dto.Username, dto.Email);

        try
        {
            var user = await _authService.Register(dto.Username, dto.Email, dto.Password);
            
            _logger.LogInformation(
                "User registered successfully. UserID: {UserId}", user.Id);
                
            return new UserDto(user.Id, user.Username, user.Email, user.CreatedAt);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for {Username}", dto.Username);
            return BadRequest(new ProblemDetails {
                Title = "Registration failed",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected registration error");
            return StatusCode(500, new ProblemDetails {
                Title = "Internal server error",
                Detail = "An unexpected error occurred"
            });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Login(UserLoginDto dto)
    {
        _logger.LogInformation("Login attempt for {Username}", dto.Username);
        
        try
        {
            var token = await _authService.Login(dto.Username, dto.Password);
            
            _logger.LogInformation("Successful login for {Username}", dto.Username);
            return new TokenResponseDto { Token = token };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "Invalid login attempt. Username: {Username}", dto.Username);
                
            return Unauthorized(new ProblemDetails {
                Title = "Authentication failed",
                Detail = "Invalid username or password"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected login error");
            return StatusCode(500, new ProblemDetails {
                Title = "Internal server error",
                Detail = "An unexpected error occurred"
            });
        }
    }
}