using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Api.Middleware.Exceptions;
using NotificationService.Application;
using NotificationService.Application.Configurations;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Bind DbContext

builder.Services.AddNpgsql<AppDbContext>(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("AuthService.Infrastructure"));

// Bind configuration

builder.Services
    .AddOptions<DbSettings>()
    .Bind(builder.Configuration.GetSection("DbSettings"));

builder.Services
    .AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection("EmailSettings"));

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("JwtSettings"));

builder.Services
    .AddOptions<NotificationSettings>()
    .Bind(builder.Configuration.GetSection("NotificationSettings"));

builder.Services
    .AddOptions<TelegramSettings>()
    .Bind(builder.Configuration.GetSection("TelegramSettings"));

builder.Services
    .AddOptions<AuthSettings>()
    .Bind(builder.Configuration.GetSection("AuthSettings"));

builder.Services
    .AddOptions<KafkaSettings>()
    .Bind(builder.Configuration.GetSection("Kafka"));


// Get public key for auth

var authUrl = builder.Configuration["AuthSettings:BaseUrl"]
              ?? throw new InvalidOperationException("AuthSettings:BaseUrl is not set");

builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(10500); });

var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

var httpClient = new HttpClient(handler);
var rsa = RSA.Create();
var publicKeyPem = await httpClient.GetStringAsync($"{authUrl}/.well-known/public-key.pem");
rsa.ImportFromPem(publicKeyPem);

// Setup auth

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "test-issuer",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "test-audience",
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    context.Token = context.Request.Cookies["accessToken"];
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully");
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer("ServiceScheme", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AuthSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["AuthSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ServiceSchemeAuth");

                var svcName = ctx.Principal?.FindFirst("service_name")?.Value ?? "unknown";

                logger.LogWarning(
                    "ServiceScheme auth failed. Scheme={Scheme}, service_name={ServiceName}, Exception={Error}",
                    ctx.Scheme,
                    svcName,
                    ctx.Exception.Message);

                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ServiceSchemeAuthValidated");

                var svcName = ctx.Principal!.FindFirst("service_name")!.Value;
                
                logger.LogInformation("ServiceScheme token validated for {ServiceName}", svcName);
                
                return Task.CompletedTask;
            }
        };
    });
;

// Setup authorization

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OnlyServices", policy =>
    {
        policy.AddAuthenticationSchemes("ServiceScheme");
        policy.RequireClaim("scope", "internal_api");
    });
});

// Setup cors

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("https://localhost:10500")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

builder.Services.AddHttpClient();

// Bind rate limiter

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();