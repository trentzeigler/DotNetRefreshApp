using DotNetRefreshApp.Models;

namespace DotNetRefreshApp.Services
{
    /// <summary>
    /// Interface for email sending functionality
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send an email
        /// </summary>
        /// <param name="email">Email draft to send</param>
        /// <returns>True if sent successfully, false otherwise</returns>
        Task<bool> SendEmailAsync(EmailDraft email);

        /// <summary>
        /// Validate an email address format
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateEmailAddress(string email);
    }
}
