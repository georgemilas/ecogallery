using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.Json;

namespace GalleryApi.service.email;

public class OAuth2EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OAuth2EmailSender> _logger;
    private readonly HttpClient _httpClient;

    public OAuth2EmailSender(IConfiguration configuration, ILogger<OAuth2EmailSender> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var from = _configuration["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From not configured");
        var user = _configuration["Smtp:User"] ?? from;

        var clientId = _configuration["Smtp:OAuth:ClientId"] ?? throw new InvalidOperationException("Smtp:OAuth:ClientId not configured");
        var clientSecret = _configuration["Smtp:OAuth:ClientSecret"] ?? throw new InvalidOperationException("Smtp:OAuth:ClientSecret not configured");
        var refreshToken = _configuration["Smtp:OAuth:RefreshToken"] ?? throw new InvalidOperationException("Smtp:OAuth:RefreshToken not configured");

        // Get access token from refresh token
        var accessToken = await GetAccessTokenAsync(clientId, clientSecret, refreshToken);

        // Create message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("", from));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        // Send via SMTP with OAuth2
        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            
            // Authenticate with OAuth2
            var oauth2 = new SaslMechanismOAuth2(user, accessToken);
            await client.AuthenticateAsync(oauth2);
            
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To} via OAuth2", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via OAuth2", to);
            throw;
        }
    }

    private async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        var tokenEndpoint = "https://oauth2.googleapis.com/token";
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        try
        {
            var response = await _httpClient.PostAsync(tokenEndpoint, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonDocument.Parse(json);
            
            if (tokenResponse.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            {
                return accessTokenElement.GetString() ?? throw new InvalidOperationException("Access token is null");
            }

            throw new InvalidOperationException("No access_token in response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain OAuth2 access token");
            throw new InvalidOperationException("Failed to obtain OAuth2 access token", ex);
        }
    }
}
