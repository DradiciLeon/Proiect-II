using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Activity_Finder.Models
{
    public class Hobby
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // Proprietăți noi care să corespundă cu UI-ul tău
        public string Category { get; set; }
        public DateTime? Date { get; set; }
        public int MaxPeople { get; set; }

        // Relația Many-to-Many (Participanții la activitate)
        public List<User> Users { get; set; } = new List<User>();
    }
}
