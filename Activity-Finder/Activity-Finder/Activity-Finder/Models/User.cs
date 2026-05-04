using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Activity_Finder.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ProfileImagePath { get; set; } // Adăugat pentru poza de profil

        // Setări profil
        public string DisplayName { get; set; }

        public string Bio { get; set; } = "";
        public string Location { get; set; } = "";

        public bool PushNotifications { get; set; } = true;
        public string DistanceUnit { get; set; } = "KM";
        public string ProfileVisibility { get; set; } = "Everyone";
        public bool ShowHobbyBadge { get; set; } = true;

        // Relații
        public virtual List<Hobby> Hobbies { get; set; } = new List<Hobby>();
        public virtual ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
    }
}
