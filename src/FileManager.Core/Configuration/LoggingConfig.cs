namespace FileManager.Core.Configuration;
public class LoggingConfig
{
    public const string SectionName = "LoggingMiddleware";

    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseBody { get; set; } = true;
    public int MaxBodyLogLength { get; set; } = 4096; // 4KB
    public string[] ExcludedPaths { get; set; } = Array.Empty<string>();
    public bool LogRequestDuration { get; set; } = true;
    public string DurationFormat { get; set; } = "0";
}