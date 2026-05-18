using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
        public string Category { get; set; }
        public DateTime? Date { get; set; }
        public int MaxPeople { get; set; }
        public string City { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;


        public double Latitude { get; set; } = 0;
        public double Longitude { get; set; } = 0;

        // Autorul Hobby-ului (Facut optional cu ? pentru a nu bloca DB)
        public int? UserId { get; set; }
        public virtual User User { get; set; }

        // Participanții (Many-to-Many)
        public virtual List<User> Users { get; set; } = new List<User>();

        [NotMapped] // <-- Această linie e "magia". Spune SQL-ului: "NU crea coloană pentru asta!"
        public string UserAverageRating { get; set; }

        [NotMapped]
        public int RemainingSpots { get; set; }
    }
}
