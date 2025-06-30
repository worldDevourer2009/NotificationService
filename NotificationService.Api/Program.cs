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

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(10500, op =>
    {
        op.UseHttps();
    } );
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();