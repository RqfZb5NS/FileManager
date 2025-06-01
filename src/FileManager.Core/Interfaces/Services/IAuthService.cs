using FileManager.Core.Entities;

namespace FileManager.Core.Interfaces.Services;
public interface IAuthService
{
    Task<User> Register(string username, string email, string password);
    Task<string> Login(string usernameOrEmail, string password);
}