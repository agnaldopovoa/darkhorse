using AspNetCoreRateLimit;
using Darkhorse.Api.Hubs;
using Darkhorse.Api.Middleware;
using Darkhorse.Application;
using Darkhorse.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Configure HTTPS dynamically if PEM paths are provided
var certPath = builder.Configuration["SSL_CERT_PATH"] ?? Environment.GetEnvironmentVariable("SSL_CERT_PATH");
var keyPath = builder.Configuration["SSL_KEY_PATH"] ?? Environment.GetEnvironmentVariable("SSL_KEY_PATH");

if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(keyPath))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        });
    });
}

// 1. Serilog configuration
builder.Host.UseSerilog((context, conf) => conf
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

// 2. Add Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Controllers & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Rate Limiting setup
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// 5. Authentication & JWT
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET");
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };

            // Allow token in query string for SignalR
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/trading"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var cacheService = context.HttpContext.RequestServices.GetRequiredService<Darkhorse.Domain.Interfaces.Services.ICacheService>();
                    var jti = context.Principal?.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti))
                    {
                        var isRevoked = await cacheService.GetAsync<bool>($"Revoked:{jti}");
                        if (isRevoked)
                        {
                            context.Fail("This token has been revoked.");
                        }
                    }
                }
            };
        });
}

// 6. CORS
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration["ALLOWED_ORIGINS"] ?? "https://localhost:5173";
    var originList = origins.Split(',', StringSplitOptions.RemoveEmptyEntries);

    options.AddDefaultPolicy(policy =>
    {
        if (originList.Contains("*"))
        {
            policy.SetIsOriginAllowed(_ => true);
        }
        else
        {
            policy.WithOrigins(originList);
        }

        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", context =>
    {
        context.Response.Redirect("/swagger");
        return Task.CompletedTask;
    });
}

app.UseCors();

app.UseIpRateLimiting();

// Enable HTTPS redirection
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<CsrfMiddleware>(); // CSRF goes after Auth
app.UseAuthorization();

app.MapControllers();
app.MapHub<TradingHub>("/hubs/trading");

app.MapGet("/health", () => "Darkhorse API OK").ExcludeFromDescription();

app.Run();
