using System.Net;
using System.Text.Json;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteAsync(context, exception);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
        var (status, code, message) = exception switch
        {
            EntityNotFoundException notFound => (HttpStatusCode.NotFound, notFound.Code, notFound.Message),
            ConflictException conflict => (HttpStatusCode.Conflict, conflict.Code, conflict.Message),
            InvalidOrderTransitionException transition => (HttpStatusCode.Conflict, transition.Code, transition.Message),
            InsufficientStockException stock => (HttpStatusCode.Conflict, stock.Code, stock.Message),
            BusinessRuleException rule => (HttpStatusCode.BadRequest, rule.Code, rule.Message),
            DomainException domain => (HttpStatusCode.BadRequest, domain.Code, domain.Message),
            _ => (HttpStatusCode.InternalServerError, "internal_error",
                environment.IsProduction() ? "An unexpected error occurred." : exception.GetBaseException().Message)
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(exception, "Domain exception {Code}", code);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        var payload = JsonSerializer.Serialize(new { code, message });
        await context.Response.WriteAsync(payload);
    }
}
