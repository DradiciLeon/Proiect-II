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

        // Relația inversă
        public List<User> Users { get; set; } = new List<User>();
    }
}
