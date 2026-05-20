using System;

namespace Activity_Finder.Models
{
    public class ChatReadStatus
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int HobbyId { get; set; }

        public DateTime LastReadAt { get; set; }
    }
}