using FileManager.Application.Services;
using FileManager.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FileManager.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}