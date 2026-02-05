using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GalleryApi.service.email;

public class BasicAuthEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BasicAuthEmailSender> _logger;

    public BasicAuthEmailSender(IConfiguration configuration, ILogger<BasicAuthEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host not configured");
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var user = _configuration["Smtp:User"] ?? throw new InvalidOperationException("Smtp:User not configured");
        var pass = _configuration["Smtp:Pass"] ?? throw new InvalidOperationException("Smtp:Pass not configured");
        var from = _configuration["Smtp:From"] ?? user;

        // Create message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("", from));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        // Send via SMTP with basic auth
        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To} via Basic Auth", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via Basic Auth", to);
            throw;
        }
    }
}
