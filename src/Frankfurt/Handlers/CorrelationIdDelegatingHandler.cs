using System.Diagnostics;
using System.Net.Http;

namespace Frankfurt.Handlers;

public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdHeader].ToString()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();

        request.Headers.Add(CorrelationIdHeader, correlationId);

        return await base.SendAsync(request, cancellationToken);
    }
}