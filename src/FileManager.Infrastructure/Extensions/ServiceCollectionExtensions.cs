using FileManager.Core.Configuration;
using FileManager.Core.Interfaces.Repositories;
using FileManager.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileManager.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Регистрация репозиториев
        var storageConfig = configuration
            .GetSection(FileStorageConfig.SectionName)
            .Get<FileStorageConfig>() ?? new FileStorageConfig();
        
        // Регистрируем LocalStorageRepository как IPublicStorageRepository
        services.AddSingleton<IPublicStorageRepository>(sp => 
            (IPublicStorageRepository)new LocalStorageRepository( // Явное приведение
                storageConfig.PublicStorageProviders.Local.RootPath));
        
        // Регистрируем LocalStorageRepository как ITempStorageRepository
        services.AddSingleton<ITempStorageRepository>(sp => 
            (ITempStorageRepository)new LocalStorageRepository( // Явное приведение
                storageConfig.TempStorageProviders.Local.RootPath));
        
        // Регистрируем LocalStorageRepository как IPrivateStorageRepository
        services.AddSingleton<IPrivateStorageRepository>(sp => 
            (IPrivateStorageRepository)new LocalStorageRepository( // Явное приведение
                storageConfig.PrivateStorageProviders.Local.RootPath));
        
        return services;
    }
}