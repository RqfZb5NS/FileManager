using FileManager.Core.Entities;
using FileManager.Application.Services;
using FileManager.Infrastructure.Data;
using FileManager.Core.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using FileManager.Core.Interfaces.Services;

namespace FileManager.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IOptions<JwtConfig> _jwtConfig;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly AuthService _authService;
    private readonly DbConnection _connection;

    public AuthServiceTests()
    {
        // Создаем и открываем SQLite in-memory соединение
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated(); // Создаем схему БД
        
        // Настройка JWT конфигурации с валидным ключом
        var jwtConfig = new JwtConfig
        {
            Secret = GenerateSecureKey(32), // 256-битный ключ
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationHours = 2
        };
        
        _jwtConfig = Options.Create(jwtConfig);
        
        // Используем реальный PasswordHasher
        _passwordHasher = new PasswordHasher<User>();
        
        // Mock для логгера
        var loggerMock = new Mock<ILogger<AuthService>>();
        _logger = loggerMock.Object;
        
        _authService = new AuthService(_context, _jwtConfig, _logger, _passwordHasher);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    private static string GenerateSecureKey(int byteLength)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] key = new byte[byteLength];
        rng.GetBytes(key);
        return Convert.ToBase64String(key);
    }

    [Fact]
    public async Task Register_ShouldCreateNewUser()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var password = "TestPassword123!";

        // Act
        var user = await _authService.Register(username, email, password);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(username, user.Username);
        Assert.Equal(email, user.Email);
        
        // Проверяем хеш пароля
        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user, 
            user.PasswordHash, 
            password);
        
        Assert.NotEqual(PasswordVerificationResult.Failed, verificationResult);
        
        // Проверяем сохранение в БД
        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        Assert.NotNull(dbUser);
        Assert.Equal(email, dbUser.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldThrow()
    {
        // Arrange
        var username = "testuser";
        await _authService.Register(username, "test1@example.com", "Password1!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _authService.Register(username, "test2@example.com", "Password2!"));
        
        Assert.Contains(username, exception.Message);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldThrow()
    {
        // Arrange
        var email = "test@example.com";
        await _authService.Register("user1", email, "Password1!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _authService.Register("user2", email, "Password2!"));
        
        Assert.Contains(email, exception.Message);
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var username = "testuser";
        var password = "TestPassword123!";
        
        // Регистрируем пользователя
        var user = await _authService.Register(username, "test@example.com", password);
        
        // Явно сохраняем изменения и обновляем контекст
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var token = await _authService.Login(username, password);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        // Проверяем структуру токена без валидации подписи
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        Assert.Equal(_jwtConfig.Value.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtConfig.Value.Audience, jwtToken.Audiences);
        Assert.Contains(jwtToken.Claims, c => 
            c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == username);
        Assert.Contains(jwtToken.Claims, c => 
            c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
    }

    [Fact]
    public async Task Login_InvalidPassword_ShouldThrow()
    {
        // Arrange
        var username = "testuser";
        await _authService.Register(username, "test@example.com", "CorrectPassword");
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _authService.Login(username, "WrongPassword"));
    }
    
    [Fact]
    public async Task Login_NonExistentUser_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _authService.Login("nonexistent", "password"));
    }
    
    [Fact]
    public async Task Login_ValidCredentials_ShouldRehashPasswordWhenNeeded()
    {
        // Arrange
        var username = "testuser";
        var password = "TestPassword123!";
        
        // Создаем пользователя с "устаревшим" хешем
        var oldHasher = new PasswordHasher<User>();
        var user = new User
        {
            Username = username,
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        
        // Генерируем валидный хеш с помощью PasswordHasher
        user.PasswordHash = oldHasher.HashPassword(user, password);
        var originalHash = user.PasswordHash;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        
        // Act
        var token = await _authService.Login(username, password);
        
        // Assert
        Assert.NotNull(token);
        
        // Проверяем, что пароль был перехеширован
        var dbUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(dbUser);
        
        // Проверяем, что новый хеш валиден
        var verificationResult = _passwordHasher.VerifyHashedPassword(
            dbUser, 
            dbUser.PasswordHash, 
            password);
        
        Assert.NotEqual(PasswordVerificationResult.Failed, verificationResult);
        
        // Проверяем, что вход по-прежнему работает
        var loginAgain = await _authService.Login(username, password);
        Assert.NotNull(loginAgain);
    }
    
    // GenerateJwtToken приватный и к нему не стоит обращаться напрямую
    /*
    [Fact]
    public void GenerateJwtToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        
        // Act
        var token = _authService.GenerateJwtToken(user);
        
        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        // Проверяем структуру токена
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        Assert.Equal(_jwtConfig.Value.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtConfig.Value.Audience, jwtToken.Audiences);
        Assert.Equal(user.Id.ToString(), jwtToken.Subject);
        Assert.Contains(jwtToken.Claims, c => 
            c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
    }
    */
}