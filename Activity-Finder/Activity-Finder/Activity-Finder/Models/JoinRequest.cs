using System;

namespace Activity_Finder.Models
{
    public class JoinRequest
    {
        public int Id { get; set; }

        public int HobbyId { get; set; }
        public Hobby Hobby { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }
}