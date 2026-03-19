using System.Net;
using System.Text.Json;
using Payments.Application.Exceptions;

namespace Payments.Api.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Erro nao tratado | Path: {Path} | Method: {Method}",
                context.Request.Path, context.Request.Method);
            await HandleAsync(context, ex);
        }
    }

    private static Task HandleAsync(HttpContext context, Exception ex)
    {
        var status = ex switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            MessageDispatchException => HttpStatusCode.BadGateway,
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.Conflict,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        var response = new
        {
            traceId = context.TraceIdentifier,
            status = (int)status,
            title = "Erro ao processar requisicao",
            detail = ex.Message,
            timestamp = DateTime.UtcNow
        };

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
