using System.IO;
using System.Text;
using System.Threading.Tasks;
using FileManager.Core.Interfaces.Repositories;
using FileManager.Infrastructure.Repositories;
using Xunit;

namespace FileManager.Tests.Repositories;

public class LocalStorageRepositoryTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly IStorageRepository _repository;

    public LocalStorageRepositoryTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), $"StorageTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRootPath);
        _repository = new LocalStorageRepository(_testRootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_ShouldCreateFile()
    {
        // Arrange
        var testPath = "test.txt";
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        await _repository.SaveFileAsync(testPath, stream);

        // Assert
        var fullPath = Path.Combine(_testRootPath, testPath);
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, File.ReadAllText(fullPath));
    }

    [Fact]
    public async Task GetFileAsync_ShouldReturnFileContent()
    {
        // Arrange
        var testPath = "test.txt";
        var content = "Test content";
        var fullPath = Path.Combine(_testRootPath, testPath);
        await File.WriteAllTextAsync(fullPath, content);

        // Act
        var resultStream = await _repository.GetFileAsync(testPath);
        using var reader = new StreamReader(resultStream);
        var result = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile()
    {
        // Arrange
        var testPath = "test.txt";
        var fullPath = Path.Combine(_testRootPath, testPath);
        await File.WriteAllTextAsync(fullPath, "Content");

        // Act
        await _repository.DeleteFileAsync(testPath);

        // Assert
        Assert.False(File.Exists(fullPath));
    }
    [Fact]
    public async Task MoveFileAsync_ShouldMoveFile()
    {
        // Arrange
        var sourcePath = "source.txt";
        var destPath = "destination.txt";
        var content = "Source content";
        var fullSourcePath = Path.Combine(_testRootPath, sourcePath);
        await File.WriteAllTextAsync(fullSourcePath, content);

        // Act
        await _repository.MoveFileAsync(sourcePath, destPath);

        // Assert
        var fullDestPath = Path.Combine(_testRootPath, destPath);
        Assert.True(File.Exists(fullDestPath));
        Assert.False(File.Exists(fullSourcePath));
        Assert.Equal(content, File.ReadAllText(fullDestPath));
    }
}