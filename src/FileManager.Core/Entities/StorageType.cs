namespace FileManager.Core.Entities;

public enum StorageType
{
    Public,     // Общее хранилище
    Private,    // Приватное (пользовательское)
    Temp        // Временное (для расшаренных файлов)
}