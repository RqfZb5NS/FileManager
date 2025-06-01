using FileManager.Api.Controllers;
using FileManager.Api.Dtos;
using FileManager.Core.Entities;
using FileManager.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FileManager.Tests.Controllers;

public class AuthControllerTests 
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerServiceMock;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerServiceMock = new Mock<ILogger<AuthController>>();
        _authController = new AuthController(
            _authServiceMock.Object, 
            _loggerServiceMock.Object
        );
    }

    [Fact]
    public async Task Register_ValidRequest_ShouldReturnUserDto()
    {
        // Arrange
        var request = new UserRegisterDto(
            "testuser", 
            "test@example.com", 
            "TestPassword123!");
        
        var registeredUser = new User 
        { 
            Id = 1, 
            Username = request.Username, 
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };
        
        _authServiceMock.Setup(s => s.Register(
                request.Username, 
                request.Email, 
                request.Password))
            .ReturnsAsync(registeredUser);

        // Act
        var result = await _authController.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        
        Assert.Equal(registeredUser.Id, userDto.Id);
        Assert.Equal(registeredUser.Username, userDto.Username);
        Assert.Equal(registeredUser.Email, userDto.Email);
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var request = new UserLoginDto(
            "testuser", 
            "TestPassword123!");
        
        var token = "some_jwt_token";

        _authServiceMock.Setup(s => s.Login(
                request.Username, 
                request.Password))
            .ReturnsAsync(token);

        // Act
        var result = await _authController.Login(request);

        // Assert
        // Исправлено: получаем OkObjectResult из Result, а затем извлекаем значение
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tokenResponse = Assert.IsType<TokenResponseDto>(okResult.Value);
        
        Assert.Equal(token, tokenResponse.Token);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UserLoginDto(
            "testuser", 
            "WrongPassword");

        _authServiceMock.Setup(s => s.Login(
                request.Username, 
                request.Password))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act
        var result = await _authController.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("Invalid credentials", unauthorizedResult.Value);
    }
}