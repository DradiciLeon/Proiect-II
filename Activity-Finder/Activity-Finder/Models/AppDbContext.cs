using System;
using Microsoft.EntityFrameworkCore;

namespace Activity_Finder.Models
{
    public class AppDbContext : DbContext
    {
        // Tabelele din baza de date
        public DbSet<User> Users { get; set; }
        public DbSet<Hobby> Hobbies { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Configurarea conexiunii către baza de date locală SQL Server
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=HobbyAppDB;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aici EF Core știe deja să facă relația Many-to-Many 
            // pentru că ai List<Hobby> în User și List<User> în Hobby.
            // Dacă vrei să fii foarte specific, poți lăsa metoda goală momentan.
            base.OnModelCreating(modelBuilder);
        }
    }
}