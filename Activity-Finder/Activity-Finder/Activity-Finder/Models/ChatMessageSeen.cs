using System;

namespace Activity_Finder.Models
{
    public class ChatMessageSeen
    {
        public int Id { get; set; }

        public int ChatMessageId { get; set; }
        public ChatMessage ChatMessage { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime SeenAt { get; set; }
    }
}