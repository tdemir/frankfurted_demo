global using System;
global using Frankfurt;
global using Frankfurt.Config;
using Frankfurt.Middlewares;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Frankfurt.Helpers;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add HttpContextAccessor
    builder.Services.AddHttpContextAccessor();

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With(new ClientIpEnricher(services.GetRequiredService<IHttpContextAccessor>()))
        .WriteTo.Console());

    // Add OpenTelemetry
    builder.Services.AddCustomOpenTelemetrySupport(serviceName: "Frankfurt.API", serviceVersion: "1.0.0");

    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
        options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
    });

    // Configure rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("FixedPolicy", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);    // Time window of 1 minute
            opt.PermitLimit = 100;                   // Allow 100 requests per minute
            opt.QueueLimit = 2;                      // Queue limit of 2
            opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        });
    });


    // Add API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    });

    // Add API version explorer
    // builder.Services.AddVersionedApiExplorer(options =>
    // {
    //     options.GroupNameFormat = "'v'VVV";
    //     options.SubstituteApiVersionInUrl = true;
    // });
    


    // Add services to the container.
    builder.Services.AddControllers();

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddMemoryCache();

    // Add configuration
    builder.Services.Configure<AppSettings>(
        builder.Configuration.GetSection("AppSettings"));

    builder.Services.AddDependencies();



    builder.Services.AddHttpClientHelpers();



    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlerMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseRateLimiter(); // Enable rate limiting globaly

    //app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();


    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

