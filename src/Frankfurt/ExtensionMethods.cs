using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frankfurt.Handlers;
using Frankfurt.Services;
using Frankfurt.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;

namespace Frankfurt;

public static class ExtensionMethods
{
    public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = true;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
    }
    
    public static void AddHttpClientHelpers(this IServiceCollection services)
    {

        // var retryPolicy = Policy.Handle<HttpRequestException>()
        //     .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var retryPolicy = HttpPolicyExtensions.HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(6, 
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                            );

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

        // var circuitBreakerPolicy = Policy.Handle<HttpRequestException>()
        //     .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

        var circuitBreakerPolicy = HttpPolicyExtensions.HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

        services.AddHttpClient("frankfurtClient", (sp, client) => {
            var settings = sp.GetRequiredService<IOptions<AppSettings>>().Value;

            client.BaseAddress = new Uri(settings.ApiBaseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(timeoutPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);

            // builder.Services.AddHttpClient(client => {
//     client.BaseAddress = new Uri("https://api.frankfurter.dev/v1/"); // Example API URL
//     client.Timeout = TimeSpan.FromSeconds(30); // Set a timeout for the HTTP requests
// }).AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)));
    }

    public static void AddDependencies(this IServiceCollection services)
    {
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICustomCacheService, CustomMemoryCacheService>();
        services.AddScoped<ICurrencyFetcher, CurrencyFetcher>();
        services.AddScoped<IUserService, UserService>();
    }

     public static void AddCustomOpenTelemetrySupport(this IServiceCollection services, string serviceName, string serviceVersion)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .AddSource(serviceName)
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName: serviceName,
                                    serviceVersion: serviceVersion))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter();
            });
    }

}