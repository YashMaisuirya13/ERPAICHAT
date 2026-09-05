using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Events.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = context.TraceIdentifier;
        
        _logger.LogInformation("Incoming Request: {Method} {Path} | CorrelationId: {CorrelationId}", 
            context.Request.Method, context.Request.Path, correlationId);

        try
        {
            await _next(context);
            sw.Stop();
            _logger.LogInformation("Outgoing Response: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds, correlationId);
        }
        catch
        {
            sw.Stop();
            _logger.LogError("Failed Request: {Method} {Path} failed after {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds, correlationId);
            throw;
        }
    }
}
