using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Activity_Finder.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int HobbyId { get; set; } // La ce activitate a participat
        public int FromUserId { get; set; } // Cine dă nota
        public int ToUserId { get; set; } // Cine primește nota (organizatorul)
        public int Stars { get; set; } // 1-5
        public DateTime CreatedAt { get; set; }

        public virtual Hobby Hobby { get; set; }
        public virtual User FromUser { get; set; }
    }
}
