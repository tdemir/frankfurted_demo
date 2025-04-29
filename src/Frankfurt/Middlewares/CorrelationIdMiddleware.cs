using System.Diagnostics;
using Serilog.Context;

namespace Frankfurt.Middlewares;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response?.Headers?.Append(CorrelationIdHeader, correlationId);
            await _next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            var tempVal = correlationId.ToString();
            if (!string.IsNullOrWhiteSpace(tempVal))
            {
                return tempVal;
            }
        }
        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }
}