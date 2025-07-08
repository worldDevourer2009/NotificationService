using System.Net.Mime;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Api.Middleware.Exceptions;
using NotificationService.Api.Middleware.Tokens;
using NotificationService.Api.Services;
using NotificationService.Application;
using NotificationService.Application.Configurations;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

using var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
});
var startupLogger = loggerFactory.CreateLogger<Program>();

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug();

// Bind DbContext
startupLogger.LogInformation("Adding DbContext");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration["DbSettings:PostgresConnection"],
        npgsql => npgsql.MigrationsAssembly("NotificationService.Infrastructure")
    )
);

startupLogger.LogInformation("DbContext added");

// Bind configuration
startupLogger.LogInformation("Binding configuration");

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

builder.Services
    .AddOptions<InternalAuthSettings>()
    .Bind(builder.Configuration.GetSection("InternalAuthSettings"));

startupLogger.LogInformation("Adding HTTP client");

var internalAuth = builder.Configuration
                       .GetSection("InternalAuthSettings")
                       .Get<InternalAuthSettings>() 
                   ?? throw new InvalidOperationException("InternalAuthSettings aren't configured");

var endpointUri = new Uri(internalAuth.Endpoint!);
var baseUri = endpointUri.GetLeftPart(UriPartial.Authority);

builder.Services
    .AddHttpClient(internalAuth.ServiceClientId!, client =>
    {
        client.BaseAddress = new Uri(baseUri);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddHttpMessageHandler<InternalAuthHandler>();

builder.Services.AddHttpClient();

startupLogger.LogInformation("HTTP client added");

// Configure Kestrel to start immediately
builder.WebHost.ConfigureKestrel(options =>
{
    if (!builder.Environment.IsDevelopment())
    {
        options.ListenAnyIP(10500);
    }
    else
    {
        options.ListenAnyIP(10503);
    }
});

// Добавляем сервис для работы с публичным ключом
builder.Services.AddSingleton<IPublicKeyService, PublicKeyService>();

// Setup auth
startupLogger.LogInformation("Setting up authentication");

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
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var publicKeyService = builder.Services.BuildServiceProvider().GetRequiredService<IPublicKeyService>();
                return new[]
                {
                    publicKeyService.GetPublicKey()
                };
            },
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
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthenticationFailed");
                logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("TokenValidated");
                logger.LogDebug("Token validated successfully");
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer("ServiceScheme", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var publicKeyService = builder.Services.BuildServiceProvider().GetRequiredService<IPublicKeyService>();
                return new[] { publicKeyService.GetPublicKey() };
            },
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

startupLogger.LogInformation("Authentication setup");

// Setup authorization
startupLogger.LogInformation("Setting up authorization");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OnlyServices", policy =>
    {
        policy.AddAuthenticationSchemes("ServiceScheme");
        policy.RequireClaim("scope", "internal_api");
    });
    
    options.AddPolicy("RequireUser", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
    
    options.AddPolicy("UserOrService", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ServiceScheme");
        policy.RequireAuthenticatedUser();
    });
});

startupLogger.LogInformation("Authorization setup");

// Setup cors
startupLogger.LogInformation("Setting up cors");

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

// Bind rate limiter
startupLogger.LogInformation("Adding rate limiter");

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

startupLogger.LogInformation("Rate limiter added");

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"));

// Add services
startupLogger.LogInformation("Adding services");

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddInternalAuthHandler();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

startupLogger.LogInformation("Services added");

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Notification Service API", 
        Version = "v1",
        Description = "API for managing notifications"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        startupLogger.LogInformation("Ensuring database is created...");
        
        var created = context.Database.EnsureCreated();
        
        if (created)
        {
            startupLogger.LogInformation("Database created successfully");
        }
        else
        {
            startupLogger.LogInformation("Database already exists");
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Error occurred while ensuring database creation");
        throw;
    }
}

app.Lifetime.ApplicationStarted.Register(() => { startupLogger.LogInformation("Application started successfully"); });

app.Lifetime.ApplicationStopping.Register(() => { startupLogger.LogInformation("Application is stopping..."); });

app.Lifetime.ApplicationStopped.Register(() => { startupLogger.LogInformation("Application stopped"); });

// Configure the HTTP request pipeline
startupLogger.LogInformation("Configuring the HTTP request pipeline");

app.UseCors("AllowAll");

app.UseMiddleware<PublicKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

startupLogger.LogInformation("HTTP request pipeline configured");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API V1");
    });
}

// Health Check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                exception = entry.Value.Exception?.Message,
                duration = entry.Value.Duration.ToString()
            })
        });
        
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

startupLogger.LogInformation("Starting the application");

app.Run();

public partial class Program;