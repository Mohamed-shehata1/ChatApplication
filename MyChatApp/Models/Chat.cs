using System.ComponentModel.DataAnnotations;

namespace MyChatApp.Models
{
    public class Chat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = "New Chat";

        public string Model { get; set; } = "gpt-4o-mini";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Message> Messages { get; set; } = new();
    }
}
