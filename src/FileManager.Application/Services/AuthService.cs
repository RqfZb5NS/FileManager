using System.Security.Claims;
using FileManager.Core.Entities;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;

using FileManager.Infrastructure.Data;
using FileManager.Core.Configuration;
using FileManager.Core.Interfaces.Services;

namespace FileManager.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtConfig _jwtConfig;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        AppDbContext context,
        IOptions<JwtConfig> jwtConfig,
        ILogger<AuthService> logger,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _jwtConfig = jwtConfig.Value;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> Register(string username, string email, string password)
    {
        _logger.LogInformation("Starting user registration for {Username}", username);
        try
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                _logger.LogWarning("Registration failed: Username '{Username}' already exists.", username);
                throw new InvalidOperationException($"User with username '{username}' already exists");
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                _logger.LogWarning("Registration failed: Email '{Email}' already exists.", email);
                throw new InvalidOperationException($"User with email '{email}' already exists");
            }

            var user = new User
            {
                Username = username,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            // Хешируем пароль с помощью PasswordHasher.HashPassword
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User registered successfully. UserID: {UserId}, Email: {Email}",
                user.Id,
                user.Email);

            return user;
        }
        catch (InvalidOperationException)
        {
            throw; 
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Registration failed for {Username}. Error: {ErrorMessage}",
                username,
                ex.Message);
            throw;
        }
    }

    public async Task<string> Login(string usernameOrEmail, string password)
    {
        _logger.LogInformation("Login attempt for {UsernameOrEmail}", usernameOrEmail);
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

            if (user is null)
            {
                _logger.LogWarning("Authentication failed for {UsernameOrEmail}: User not found.", usernameOrEmail);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            _logger.LogDebug("User found. Verifying password for {UsernameOrEmail}...", usernameOrEmail);

            // Проверяем пароль с помощью PasswordHasher.VerifyHashedPassword
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Authentication failed for {UsernameOrEmail}: Invalid password.", usernameOrEmail);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Если требуется перехеширование (например, из-за изменения алгоритма, итераций и т.д.)
            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                _logger.LogInformation("Password rehash needed for user {UserId}. Updating hash...", user.Id);
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Successful login for {UsernameOrEmail}", usernameOrEmail);
            return GenerateJwtToken(user);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Логирование уже произошло выше
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Login failed for {UsernameOrEmail}. Error: {ErrorMessage}",
                usernameOrEmail,
                ex.Message);
            throw;
        }
    }

    private string GenerateJwtToken(User user)
    {
        try
        {
            _logger.LogDebug("Generating JWT token for UserId: {UserId}", user.Id);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.Username),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(_jwtConfig.ExpirationHours);

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogInformation("JWT generated for user {Username}", user.Username);
            return jwt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT for user {UserId}. Error: {ErrorMessage}", user.Id, ex.Message);
            throw;
        }
    }
}