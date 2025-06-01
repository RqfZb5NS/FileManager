using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FileManager.Core.Interfaces.Repositories;

namespace FileManager.Infrastructure.Repositories;


public class LocalStorageRepository : IStorageRepository
{
    private readonly string _rootPath;

    public LocalStorageRepository(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Root path cannot be null or empty", nameof(rootPath));
        
        _rootPath = Path.GetFullPath(rootPath);
        
        // Защита от корневых директорий системы
        if (Path.GetPathRoot(_rootPath) == _rootPath)
        {
            throw new InvalidOperationException(
                "Using system root directory as storage root is not allowed");
        }
        
        EnsureDirectoryExists(_rootPath);
    }

    private void EnsureDirectoryExists(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create directory: {path}", ex);
        }
    }

    private string GetFullPath(string path)
    {
        // Защита от null и пустых значений
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        // Нормализация пути
        var sanitizedPath = path.Replace('/', Path.DirectorySeparatorChar)
                            .Replace('\\', Path.DirectorySeparatorChar);

        // Получение абсолютного пути
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, sanitizedPath));
        
        // Проверка на выход за пределы корневой директории
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Invalid path: Attempt to access restricted directory. " +
                $"Root: {_rootPath}, Requested: {fullPath}");
        }
        
        return fullPath;
    }

    public async Task SaveFileAsync(string path, Stream content)
    {
        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        EnsureDirectoryExists(directory);
        
        await using var fileStream = new FileStream(fullPath, FileMode.Create);
        await content.CopyToAsync(fileStream);
    }

    public async Task<Stream> GetFileAsync(string path)
    {
        var fullPath = GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");

        // Используем MemoryStream для безопасного чтения
        var memoryStream = new MemoryStream();
        await using (var fileStream = new FileStream(fullPath, FileMode.Open))
        {
            await fileStream.CopyToAsync(memoryStream);
        }
        memoryStream.Position = 0;
        return memoryStream;
    }

    public Task DeleteFileAsync(string path)
    {
        var fullPath = GetFullPath(path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        
        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string path)
    {
        var fullPath = GetFullPath(path);
        EnsureDirectoryExists(fullPath);
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string path, bool recursive = false)
    {
        var fullPath = GetFullPath(path);
        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive);
        
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string path)
    {
        var fullPath = GetFullPath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<bool> DirectoryExistsAsync(string path)
    {
        var fullPath = GetFullPath(path);
        return Task.FromResult(Directory.Exists(fullPath));
    }

    public Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        var fullSource = GetFullPath(sourcePath);
        var fullDestination = GetFullPath(destinationPath);
        
        if (!File.Exists(fullSource))
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        
        var destinationDir = Path.GetDirectoryName(fullDestination)!;
        EnsureDirectoryExists(destinationDir);
        
        File.Copy(fullSource, fullDestination, overwrite: true);
        return Task.CompletedTask;
    }

    public Task MoveFileAsync(string sourcePath, string destinationPath)
    {
        var fullSource = GetFullPath(sourcePath);
        var fullDestination = GetFullPath(destinationPath);
        
        if (!File.Exists(fullSource))
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        
        var destinationDir = Path.GetDirectoryName(fullDestination)!;
        EnsureDirectoryExists(destinationDir);
        
        File.Move(fullSource, fullDestination, overwrite: true);
        return Task.CompletedTask;
    }

    public Task<long> GetFileSizeAsync(string path)
    {
        var fullPath = GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");
        
        return Task.FromResult(new FileInfo(fullPath).Length);
    }

    public async Task<string> GetFileHashAsync(string path)
    {
        var fullPath = GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");
        
        await using var stream = File.OpenRead(fullPath);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}