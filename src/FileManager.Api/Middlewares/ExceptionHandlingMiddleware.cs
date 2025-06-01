using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using FileManager.Core.Configuration;

namespace FileManager.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly ExceptionHandlingConfig _config;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IOptions<ExceptionHandlingConfig> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (_config.LogFullException)
            {
                _logger.LogError(ex, "An error occurred");
            }
            else
            {
                _logger.LogError("An error occurred: {Message}", ex.Message);
            }
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var environment = context.RequestServices.GetService<IHostEnvironment>();
        bool isDevelopment = environment?.IsDevelopment() == true;

        var response = new
        {
            error = _config.DefaultErrorMessage,
            message = exception.Message,
            stackTrace = (_config.IncludeStackTrace || isDevelopment) 
                ? exception.StackTrace 
                : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}