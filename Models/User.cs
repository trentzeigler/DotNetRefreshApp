using System.ComponentModel.DataAnnotations;

namespace DotNetRefreshApp.Models
{
    /// <summary>
    /// Represents a user in the system with authentication
    /// </summary>
    public class User
    {
        /// <summary>
        /// Primary key for the database table
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User's full name
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User's email address (unique)
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt hashed password
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's role (e.g., Admin, User)
        /// </summary>
        public string Role { get; set; } = "User";

        /// <summary>
        /// Whether the user's email has been verified
        /// </summary>
        public bool EmailVerified { get; set; } = false;

        /// <summary>
        /// Email verification token
        /// </summary>
        public string? EmailVerificationToken { get; set; }

        /// <summary>
        /// When the user account was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
