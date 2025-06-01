namespace FileManager.Core.Configuration;
public class ExceptionHandlingConfig
{
    public const string SectionName = "ExceptionHandlingMiddleware";

    public bool IncludeStackTrace { get; set; } = false;
    public bool LogFullException { get; set; } = true;
    public string DefaultErrorMessage { get; set; } = "Internal Server Error";
}