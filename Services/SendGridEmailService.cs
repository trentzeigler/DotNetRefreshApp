using DotNetRefreshApp.Models;
using System.Net.Mail;
using System.Text.RegularExpressions;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DotNetRefreshApp.Services
{
    /// <summary>
    /// SendGrid implementation of email service
    /// </summary>
    public class SendGridEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Send an email using SendGrid API
        /// </summary>
        public async Task<bool> SendEmailAsync(EmailDraft email)
        {
            try
            {
                var apiKey = _configuration["SENDGRID_API_KEY"] ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY");

                if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("your-sendgrid-api-key"))
                {
                    _logger.LogWarning("SendGrid API key not configured. Email not sent.");
                    throw new InvalidOperationException("SendGrid API key not configured. Please set SENDGRID_API_KEY in your .env file.");
                }

                // Validate email addresses
                if (!ValidateEmailAddress(email.To))
                {
                    throw new ArgumentException($"Invalid recipient email address: {email.To}");
                }

                if (!ValidateEmailAddress(email.From))
                {
                    throw new ArgumentException($"Invalid sender email address: {email.From}");
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(email.From);
                var to = new EmailAddress(email.To);
                var msg = MailHelper.CreateSingleEmail(from, to, email.Subject, email.Body, email.IsHtml ? email.Body : null);
                var response = await client.SendEmailAsync(msg);
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogInformation($"SendGrid response status: {response.StatusCode}");
                _logger.LogInformation($"SendGrid response body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email sent successfully to: {email.To}, Subject: {email.Subject}");
                }
                else
                {
                    _logger.LogError($"Failed to send email. Status: {response.StatusCode}, Body: {responseBody}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email via SendGrid");
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
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
