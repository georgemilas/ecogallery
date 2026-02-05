using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GalleryApi.service.email;

public class SendGridEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailSender> _logger;
    private readonly HttpClient _httpClient;

    public SendGridEmailSender(IConfiguration configuration, ILogger<SendGridEmailSender> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var apiKey = _configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid:ApiKey not configured");
        var from = _configuration["SendGrid:From"] ?? throw new InvalidOperationException("SendGrid:From not configured");

        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email = to } },
                    subject
                }
            },
            from = new { email = from },
            content = new[]
            {
                new { type = "text/plain", value = body }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SendGrid email failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, responseBody);
                response.EnsureSuccessStatusCode();
            }

            _logger.LogInformation("Email sent successfully to {To} via SendGrid", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via SendGrid", to);
            throw;
        }
    }
}
