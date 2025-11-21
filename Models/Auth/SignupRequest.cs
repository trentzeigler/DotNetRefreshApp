using System.ComponentModel.DataAnnotations;

namespace DotNetRefreshApp.Models.Auth
{
    public class SignupRequest
    {
        [Required]
        [MinLength(2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(10, ErrorMessage = "Password must be at least 10 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
