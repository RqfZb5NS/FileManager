namespace FileManager.Core.Configuration;
public class DatabaseConfig
{
    public const string SectionName = "ConnectionStrings";
    
    public string Default { get; set; } = string.Empty;
}