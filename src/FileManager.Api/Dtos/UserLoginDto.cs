using System.ComponentModel.DataAnnotations;

namespace FileManager.Api.Dtos;

public record UserLoginDto(
    [Required] string Username,
    [Required] string Password);