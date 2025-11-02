using System.ComponentModel.DataAnnotations;

namespace MyChatApp.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string Role { get; set; } = "user"; // user, assistant, system

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ChatId { get; set; }
        public Chat? Chat { get; set; }
    }
}
