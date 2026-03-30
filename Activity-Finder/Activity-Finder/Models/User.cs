using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Activity_Finder.Models
{
    public class User
    {
        public int Id { get; set; } // Devine automat Primary Key
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // În producție, aici se pune un hash, nu parola în clar

        // Relația: Un user poate avea mai multe hobby-uri
        public List<Hobby> Hobbies { get; set; } = new List<Hobby>();
    }
}
