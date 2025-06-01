using System.ComponentModel.DataAnnotations;

namespace FileManager.Api.Dtos;

public record UserRegisterDto(
    [Required][MinLength(3)] string Username,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password);