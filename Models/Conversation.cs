using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotNetRefreshApp.Models
{
    // Represents a complete conversation with an LLM
    public class Conversation
    {
        // Primary key
        public int Id { get; set; }

        // Unique identifier for the conversation (used by frontend)
        [Required]
        public string ConversationId { get; set; } = string.Empty;

        // Foreign key to associate conversation with a user
        [Required]
        public int UserId { get; set; }

        // JSON array of messages in this conversation
        // Stored as JSON to keep all messages together in one record
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string MessagesJson { get; set; } = "[]";

        // Timestamp when conversation was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Timestamp when conversation was last updated
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property to User
        public User? User { get; set; }

        // Helper property to work with messages as objects (not stored in DB)
        [NotMapped]
        public List<ConversationMessageDto> Messages
        {
            get => System.Text.Json.JsonSerializer.Deserialize<List<ConversationMessageDto>>(MessagesJson) ?? new List<ConversationMessageDto>();
            set => MessagesJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }

    // DTO for individual messages within a conversation
    public class ConversationMessageDto
    {
        public string MessageText { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty; // "user", "assistant", or "system"
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
