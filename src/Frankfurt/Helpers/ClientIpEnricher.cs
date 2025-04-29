using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace Frankfurt.Helpers;

public class ClientIpEnricher : ILogEventEnricher
{
    private const string IpAddressPropertyName = "ClientIp";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientIpEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(clientIp)) return;

        var clientIpProperty = propertyFactory.CreateProperty(IpAddressPropertyName, clientIp);
        logEvent.AddPropertyIfAbsent(clientIpProperty);
    }
}