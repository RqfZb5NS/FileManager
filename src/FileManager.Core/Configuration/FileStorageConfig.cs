namespace FileManager.Core.Configuration;
public class FileStorageConfig
{
    public const string SectionName = "FileStorage";
    
    public string PublicStorageProvider { get; set; } = "Local";
    public string TempStorageProvider { get; set; } = "Local";
    public string[] AllowedMimeTypes { get; set; } = Array.Empty<string>();
    public long MaxFileSize { get; set; } = 104857600; // 100 MB
    
    public PublicStorageProviders PublicStorageProviders { get; set; } = new();
    public TempStorageProviders TempStorageProviders { get; set; } = new();
    public PrivateStorageProviders PrivateStorageProviders { get; set; } = new();
}

public class PublicStorageProviders
{
    public LocalPublicStorageConfig Local { get; set; } = new();
    public S3StorageConfig S3 { get; set; } = new();
    public AzureStorageConfig Azure { get; set; } = new();
}

public class TempStorageProviders
{
    public LocalTempStorageConfig Local { get; set; } = new();
    public S3TempStorageConfig S3 { get; set; } = new();
    public AzureTempStorageConfig Azure { get; set; } = new();
}

public class PrivateStorageProviders
{
    public LocalTempStorageConfig Local { get; set; } = new();
    public S3TempStorageConfig S3 { get; set; } = new();
    public AzureTempStorageConfig Azure { get; set; } = new();
}

public class LocalPublicStorageConfig
{
    public string RootPath { get; set; } = "./FileStorage";
}

public class LocalTempStorageConfig
{
    public string RootPath { get; set; } = "./TempFileStorage";
}

public class S3StorageConfig
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public class S3TempStorageConfig
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
}

public class AzureStorageConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = "files";
}

public class AzureTempStorageConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = "temp-files";
}