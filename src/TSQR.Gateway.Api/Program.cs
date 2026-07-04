using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
var jwtIssuer = jwtSection["Issuer"] ?? "tsqr-autheo";
var jwtAudience = jwtSection["Audience"] ?? "tsqr-services";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // In Docker the gateway listens on plain HTTP inside the container.
        // RequireHttpsMetadata would reject every request there. Disable it
        // explicitly; TLS termination is the job of an upstream ingress.
        options.RequireHttpsMetadata = builder.Environment.IsDevelopment()
            ? false
            : false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

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
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapControllers();

app.Run();