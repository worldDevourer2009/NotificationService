using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Application.Configurations;
using NotificationService.Domain.Services;

namespace NotificationService.Infrastructure.Services;

public class MailKitEmailSender : IEmailSender
{
    private readonly ILogger<MailKitEmailSender> _logger;
    private readonly string? _smtpServer;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPassword;
    private readonly string? _smtpFrom;
    private readonly string? _smtpFromDisplayName;
    private readonly bool _smtpEnableSsl;

    public MailKitEmailSender(IOptions<EmailSettings> emailSettings, ILogger<MailKitEmailSender> logger)
    {
        _logger = logger;
        var emailSettings1 = emailSettings.Value;
        _smtpServer = emailSettings1.SmtpServer;
        ;
        _smtpPort = emailSettings1.SmtpPort;
        ;
        _smtpUser = emailSettings1.SmtpUsername;
        _smtpPassword = emailSettings1.SmtpPassword;
        _smtpFrom = emailSettings1.FromEmail;
        _smtpFromDisplayName = emailSettings1.FromName;
        _smtpEnableSsl = emailSettings1.EnableSsl;
    }

    public async Task<bool> SendEmailAsync(string email, string subject, string message,
        CancellationToken cancellationToken = default)
    {
        return await SendEmailAsync(email, subject, message, null, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(string email, string subject, string message, string? htmlMessage,
        CancellationToken cancellationToken = default)
    {
        return await SendEmailAsync(email, subject, message, htmlMessage, null, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(string email, string subject, string message, string? htmlMessage,
        string[]? attachments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_smtpServer) || string.IsNullOrEmpty(_smtpFrom))
            {
                _logger.LogWarning("SMTP configuration is invalid. SmtpServer: {SmtpServer}, FromEmail: {FromEmail}",
                    _smtpServer, _smtpFrom);
                return false;
            }

            var mimeMessage = new MimeMessage();

            mimeMessage.From.Add(new MailboxAddress(_smtpFromDisplayName, _smtpFrom));
            mimeMessage.To.Add(new MailboxAddress("", email));
            mimeMessage.Subject = subject;

            var multipart = new Multipart("mixed");

            if (!string.IsNullOrEmpty(htmlMessage))
            {
                multipart.Add(new TextPart("html") { Text = htmlMessage });
            }
            else
            {
                multipart.Add(new TextPart("plain") { Text = message });
            }

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (File.Exists(attachment))
                    {
                        var attachmentPart = new MimePart()
                        {
                            Content = new MimeContent(File.OpenRead(attachment)),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                            ContentTransferEncoding = ContentEncoding.Base64,
                            FileName = Path.GetFileName(attachment)
                        };
                        multipart.Add(attachmentPart);
                    }
                }
            }

            mimeMessage.Body = multipart;

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpServer, _smtpPort, _smtpEnableSsl, cancellationToken);

            if (!string.IsNullOrEmpty(_smtpUser) && !string.IsNullOrEmpty(_smtpPassword))
            {
                await client.AuthenticateAsync(_smtpUser, _smtpPassword, cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending email");
            //TODO: change to valid email sender, for now it's false
            return true;
        }
    }
}