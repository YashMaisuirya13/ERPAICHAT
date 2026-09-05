using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Events.Exceptions;
using Shared.Events.Models;

namespace Shared.Events.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.TraceIdentifier;
            _logger.LogError(ex, "An unhandled exception occurred during request execution. CorrelationId: {CorrelationId}", correlationId);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var response = context.Response;

        var (statusCode, message, errors) = exception switch
        {
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message, new List<string>()),
            BadRequestException badRequest => (HttpStatusCode.BadRequest, badRequest.Message, badRequest.Errors),
            UnauthorizedException unauthorized => (HttpStatusCode.Unauthorized, unauthorized.Message, new List<string>()),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, forbidden.Message, new List<string>()),
            KeyNotFoundException keyNotFound => (HttpStatusCode.NotFound, keyNotFound.Message, new List<string>()),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message, new List<string>()),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred.", new List<string> { exception.Message })
        };

        response.StatusCode = (int)statusCode;

        var apiResponse = ApiResponse.Failed(message, errors);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(apiResponse, options);

        await response.WriteAsync(json);
    }
}
