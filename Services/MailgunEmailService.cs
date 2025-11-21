using DotNetRefreshApp.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotNetRefreshApp.Services
{
public class MailgunEmailService : IEmailService
{
private readonly IConfiguration _configuration;
private readonly ILogger<MailgunEmailService> _logger;
private readonly HttpClient _httpClient;

    public MailgunEmailService(IConfiguration configuration, ILogger<MailgunEmailService> logger, HttpClient httpClient)  
    {  
        _configuration = configuration;  
        _logger = logger;  
        _httpClient = httpClient;  
    }  

    public async Task<bool> SendEmailAsync(EmailDraft email)  
    {  
        try  
        {  
            var apiKey = _configuration["MAILGUN_API_KEY"] ?? Environment.GetEnvironmentVariable("MAILGUN_API_KEY");  
            var domain = _configuration["MAILGUN_DOMAIN"] ?? Environment.GetEnvironmentVariable("MAILGUN_DOMAIN");  

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(domain))  
            {  
                _logger.LogWarning("Mailgun API key or domain not configured.");  
                throw new InvalidOperationException("Mailgun API key or domain missing. Please configure MAILGUN_API_KEY and MAILGUN_DOMAIN.");  
            }  

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.mailgun.net/v3/{domain}/messages");  
            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}"));  
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);  

            var formData = new Dictionary<string, string>  
            {  
                ["from"] = email.From,  
                ["to"] = email.To,  
                ["subject"] = email.Subject,  
                ["text"] = email.IsHtml ? null : email.Body,  
                ["html"] = email.IsHtml ? email.Body : null  
            };  

            request.Content = new FormUrlEncodedContent(formData);  
            var response = await _httpClient.SendAsync(request);  
            var responseBody = await response.Content.ReadAsStringAsync();  

            if (response.IsSuccessStatusCode)  
            {  
                _logger.LogInformation($"Email sent successfully via Mailgun to {email.To}");  
                return true;  
            }  
            else  
            {  
                _logger.LogError($"Failed to send email via Mailgun. Status: {response.StatusCode}, Body: {responseBody}");  
                return false;  
            }  
        }  
        catch (Exception ex)  
        {  
            _logger.LogError(ex, "Error sending email via Mailgun");  
            throw;  
        }
    }

        /// <summary>
        /// Validate email address format using regex
        /// </summary>
        public bool ValidateEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use MailAddress to validate
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
