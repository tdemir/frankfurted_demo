using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Frankfurt.Middlewares;

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
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response?.StatusCode;
            var method = context.Request?.Method;
            var path = context.Request?.Path.Value;
            var clientId = GetClientId(context);

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms - ClientId: {ClientId}",
                method,
                path,
                statusCode,
                sw.ElapsedMilliseconds,
                clientId ?? "anonymous");
        }
    }

    private string? GetClientId(HttpContext context)
    {
        // Try to get client_id from claims
        var clientId = context.User?.FindFirst("client_id")?.Value;
        if (!string.IsNullOrEmpty(clientId))
            return clientId;

        // Alternative: try to get sub claim
        clientId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(clientId))
            return clientId;

        // If no claims found, try to get from bearer token
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                clientId = jwtToken.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error parsing JWT token");
            }
        }

        return clientId;
    }
}