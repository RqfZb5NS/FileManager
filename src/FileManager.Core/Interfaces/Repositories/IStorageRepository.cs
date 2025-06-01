using System.IO;
using System.Threading.Tasks;

namespace FileManager.Core.Interfaces.Repositories;
public interface IStorageRepository
{
    // Основные файловые операции
    Task SaveFileAsync(string path, Stream content);
    Task<Stream> GetFileAsync(string path);
    Task DeleteFileAsync(string path);
    
    // Операции с директориями
    Task CreateDirectoryAsync(string path);
    Task DeleteDirectoryAsync(string path, bool recursive = false);
    
    // Проверки существования
    Task<bool> FileExistsAsync(string path);
    Task<bool> DirectoryExistsAsync(string path);
    
    // Управление файлами
    Task CopyFileAsync(string sourcePath, string destinationPath);
    Task MoveFileAsync(string sourcePath, string destinationPath);
    
    // Информация о файлах
    Task<long> GetFileSizeAsync(string path);
    Task<string> GetFileHashAsync(string path);
}