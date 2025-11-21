using System.ComponentModel.DataAnnotations;

namespace DotNetRefreshApp.Models
{
    // Represents a user in the system.
    // This class will be mapped to a "Users" table in the database by Entity Framework Core.
    public class Record
    {
        // The primary key for the database table.
        public int Id { get; set; }

        // The user's full name.
        // [Required] is a Data Annotation that ensures this field cannot be null.
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; } = int.MinValue;
    }
}
