using System;

namespace Activity_Finder.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int HobbyId { get; set; }
        public Hobby Hobby { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Message { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}