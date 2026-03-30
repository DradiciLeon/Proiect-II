using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Activity_Finder.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Hobby> Hobbies { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // "localdb" este un server SQL care vine la pachet cu Visual Studio
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=HobbyAppDB;Trusted_Connection=True;");
        }
    }
}
