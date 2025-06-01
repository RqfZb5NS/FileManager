using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FileManager.Core.Configuration;

namespace FileManager.Api.Middlewares;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly LoggingConfig _config;

    public LoggingMiddleware(
        RequestDelegate next,
        ILogger<LoggingMiddleware> logger,
        IOptions<LoggingConfig> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        string requestLog = string.Empty;
        if (_config.LogRequestBody)
        {
            requestLog = await FormatRequest(context.Request);
        }
        else
        {
            requestLog = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
        }

        _logger.LogInformation("Request: {Request}", requestLog);

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var startTime = DateTime.UtcNow;
        await _next(context);
        var elapsed = DateTime.UtcNow - startTime;

        string responseLog = string.Empty;
        if (_config.LogResponseBody)
        {
            responseLog = await FormatResponse(context.Response);
        }
        else
        {
            responseLog = $"HTTP {context.Response.StatusCode}";
        }

        string durationInfo = _config.LogRequestDuration
            ? $" | Time: {elapsed.TotalMilliseconds.ToString(_config.DurationFormat)}ms"
            : string.Empty;

        _logger.LogInformation("Response: {Response}{Duration}", responseLog, durationInfo);

        await responseBody.CopyToAsync(originalBodyStream);
    }

    private bool ShouldSkipLogging(PathString path)
    {
        return _config.ExcludedPaths.Any(p => 
            path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();
        var body = await ReadBodyAsync(request.Body, _config.MaxBodyLogLength);
        request.Body.Position = 0;

        return $"{request.Method} {request.Path}{request.QueryString} {body}";
    }

    private async Task<string> FormatResponse(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var body = await ReadBodyAsync(response.Body, _config.MaxBodyLogLength);
        response.Body.Seek(0, SeekOrigin.Begin);
        
        return $"HTTP {response.StatusCode} {body}";
    }

    private async Task<string> ReadBodyAsync(Stream bodyStream, int maxLength)
    {
        using var reader = new StreamReader(bodyStream, Encoding.UTF8, 
            detectEncodingFromByteOrderMarks: false, 
            bufferSize: 1024, 
            leaveOpen: true);
        
        var buffer = new char[Math.Min(maxLength, 4096)];
        var totalRead = 0;
        var bodyBuilder = new StringBuilder();

        while (totalRead < maxLength)
        {
            var bytesRead = await reader.ReadAsync(buffer, 0, Math.Min(buffer.Length, maxLength - totalRead));
            if (bytesRead == 0) break;
            
            bodyBuilder.Append(buffer, 0, bytesRead);
            totalRead += bytesRead;
        }

        if (totalRead >= maxLength)
        {
            bodyBuilder.Append("... [TRUNCATED]");
        }

        return bodyBuilder.ToString();
    }
}