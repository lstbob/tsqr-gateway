using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TSQR.Common.WebApi;
using TSQR.Gateway.Api.Observability;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(json =>
    {
        json.IncludeScopes = true;
        json.UseUtcTimestamp = true;
        json.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    });
}

var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"]
    ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("tsqr-gateway"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("tsqr-gateway");

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(exporter =>
                exporter.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(exporter =>
                exporter.Endpoint = new Uri(otlpEndpoint));
        }
    });

if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.ParseStateValues = true;
        logging.AddOtlpExporter(exporter =>
            exporter.Endpoint = new Uri(otlpEndpoint));
    });
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.ConfigureHttpClientDefaults(httpClient =>
    httpClient.AddHttpMessageHandler<CorrelationIdHandler>());

builder.Services.AddTsqrJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Named HTTP clients used by the BffController so it is not pinned to
// hard-coded localhost URLs. Addresses come from configuration ("Services"
// section) and are overridden per environment via appsettings.Docker.json.
builder.Services.AddHttpClient("autheo", c =>
{
    var url = builder.Configuration["Services:Autheo"] ?? "http://localhost:5068";
    c.BaseAddress = new Uri(url);
});
builder.Services.AddHttpClient("tool-lib", c =>
{
    var url = builder.Configuration["Services:ToolLib"] ?? "http://localhost:5079";
    c.BaseAddress = new Uri(url);
});
builder.Services.AddHttpClient("support", c =>
{
    var url = builder.Configuration["Services:Support"] ?? "http://localhost:5070";
    c.BaseAddress = new Uri(url);
});
builder.Services.AddHttpClient("soup-kitchen", c =>
{
    var url = builder.Configuration["Services:SoupKitchen"] ?? "http://localhost:5080";
    c.BaseAddress = new Uri(url);
});
builder.Services.AddHttpClient("communities", c =>
{
    var url = builder.Configuration["Services:Communities"] ?? "http://localhost:5090";
    c.BaseAddress = new Uri(url);
});
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapControllers();

app.Run();