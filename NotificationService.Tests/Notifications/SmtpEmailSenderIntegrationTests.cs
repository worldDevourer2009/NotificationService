using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;
using NotificationService.Infrastructure.Services;
using Xunit.Abstractions;

namespace NotificationService.Tests.Notifications;

public class SmtpEmailSenderIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;

    public SmtpEmailSenderIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendEmailAsync_WithValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        var emailSettings = Options.Create(new EmailSettings
        {
            SmtpServer = "sandbox.smtp.mailtrap.io",
            SmtpPort = 2525,
            SmtpUsername = "5d0308f8286c0e",
            SmtpPassword = "10e7040d944199",
            FromEmail = "from@example.com",
            FromName = "TaskHandler App",
            EnableSsl = false
        });
        var logger = _serviceProvider.GetRequiredService<ILogger<MailKitEmailSender>>();
        var emailSender = new MailKitEmailSender(emailSettings, logger);

        // Act
        var result = await emailSender.SendEmailAsync(
            "recipient@example.com",
            "Test Subject",
            "Test message body"
        );

        // Assert
        _output.WriteLine($"Email sending test result: {result}");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailAsync_WithHtmlMessage_ShouldReturnTrue()
    {
        // Arrange
        var emailSettings = Options.Create(new EmailSettings
        {
            SmtpServer = "sandbox.smtp.mailtrap.io",
            SmtpPort = 2525,
            SmtpUsername = "5d0308f8286c0e",
            SmtpPassword = "10e7040d944199",
            FromEmail = "from@example.com",
            FromName = "TaskHandler App",
            EnableSsl = false
        });

        var logger = _serviceProvider.GetRequiredService<ILogger<MailKitEmailSender>>();
        var emailSender = new MailKitEmailSender(emailSettings, logger);

        // Act
        var result = await emailSender.SendEmailAsync(
            "recipient@example.com",
            "Test HTML Subject",
            "Plain text message",
            "<html><body><h1>HTML Message</h1></body></html>"
        );

        // Assert
        _output.WriteLine($"HTML email sending test result: {result}");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailAsync_WithAttachments_ShouldReturnTrue()
    {
        // Arrange
        var emailSettings = Options.Create(new EmailSettings
        {
            SmtpServer = "sandbox.smtp.mailtrap.io",
            SmtpPort = 2525,
            SmtpUsername = "5d0308f8286c0e",
            SmtpPassword = "10e7040d944199",
            FromEmail = "from@example.com",
            FromName = "TaskHandler App",
            EnableSsl = false
        });

        var logger = _serviceProvider.GetRequiredService<ILogger<MailKitEmailSender>>();
        var emailSender = new MailKitEmailSender(emailSettings, logger);

        // Create test file
        var testFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(testFilePath, "Test attachment content");

        var result = false;

        try
        {
            // Act - Правильно передаем attachments
            result = await emailSender.SendEmailAsync(
                "recipient@example.com",
                "Test Subject with Attachment",
                "Test message with attachment",
                null, // htmlMessage
                new[] { testFilePath } // attachments
            );
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }

        // Assert
        _output.WriteLine($"Email with attachment sending test result: {result}");
        Assert.True(result);
    }

    [Fact]
    public async Task SendEmailAsync_WithInvalidConfiguration_ShouldReturnFalse()
    {
        // Arrange
        var emailSettings = Options.Create(new EmailSettings
        {
            SmtpServer = null,
            SmtpPort = 2525,
            SmtpUsername = "test-user",
            SmtpPassword = "test-password",
            FromEmail = null,
            FromName = "Test Sender",
            EnableSsl = true
        });

        var logger = _serviceProvider.GetRequiredService<ILogger<MailKitEmailSender>>();
        var emailSender = new MailKitEmailSender(emailSettings, logger);

        // Act
        var result = await emailSender.SendEmailAsync(
            "recipient@example.com",
            "Test Subject",
            "Test message body"
        );

        // Assert
        _output.WriteLine($"Invalid configuration test result: {result}");
        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_WithValidConfigurationButFakeCredentials_ShouldReturnFalse()
    {
        // Arrange
        var emailSettings = Options.Create(new EmailSettings
        {
            SmtpServer = "fake-smtp-server.com",
            SmtpPort = 587,
            SmtpUsername = "fake-user",
            SmtpPassword = "fake-password",
            FromEmail = "fake@example.com",
            FromName = "Fake Sender",
            EnableSsl = true
        });

        var logger = _serviceProvider.GetRequiredService<ILogger<MailKitEmailSender>>();
        var emailSender = new MailKitEmailSender(emailSettings, logger);

        // Act
        var result = await emailSender.SendEmailAsync(
            "recipient@example.com",
            "Test Subject",
            "Test message body"
        );

        // Assert
        _output.WriteLine($"Fake credentials test result: {result}");
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}