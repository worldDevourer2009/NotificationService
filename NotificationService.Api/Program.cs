using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Application;
using NotificationService.Application.Configurations;
using NotificationService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

var authUrl = builder.Configuration["AuthSettings:BaseUrl"]
              ?? throw new InvalidOperationException("AuthSettings:BaseUrl is not set");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

var httpClient = new HttpClient(handler);
var rsa = RSA.Create();
var publicKeyPem = await httpClient.GetStringAsync($"{authUrl}/.well-known/public-key.pem");
rsa.ImportFromPem(publicKeyPem);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });

builder.Services.AddAuthorization();

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

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();