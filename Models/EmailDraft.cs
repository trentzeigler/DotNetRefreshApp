namespace DotNetRefreshApp.Models
{
    /// <summary>
    /// Represents an email draft or email to be sent
    /// </summary>
    public class EmailDraft
    {
        /// <summary>
        /// Recipient email address
        /// </summary>
        public string To { get; set; } = string.Empty;

        /// <summary>
        /// Email subject line
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email body content
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Whether the body is HTML formatted
        /// </summary>
        public bool IsHtml { get; set; } = false;

        /// <summary>
        /// Optional CC recipients (semicolon separated)
        /// </summary>
        public string? Cc { get; set; }

        /// <summary>
        /// Optional BCC recipients (semicolon separated)
        /// </summary>
        public string? Bcc { get; set; }

        /// <summary>
        /// Sender email address (from User model)
        /// </summary>
        public string From { get; set; } = string.Empty;
    }
}
