using System;
using Microsoft.EntityFrameworkCore;


namespace Activity_Finder.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Hobby> Hobbies { get; set; }
        public DbSet<UserInterest> UserInterests { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<JoinRequest> JoinRequests { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<ChatReadStatus> ChatReadStatuses { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<ChatMessageSeen> ChatMessageSeens { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=HobbyAppDB;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurează Autorul
            modelBuilder.Entity<Hobby>()
                .HasOne(h => h.User)
                .WithMany(u => u.Hobbies)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict); // OBLIGATORIU: Previne crash-ul la Login/Register

            // Configurează Participanții (Many-to-Many)
            modelBuilder.Entity<Hobby>()
                .HasMany(h => h.Users)
                .WithMany()
                .UsingEntity(j => j.ToTable("HobbyParticipants"));
            modelBuilder.Entity<ChatMessageSeen>()
    .HasOne(x => x.ChatMessage)
    .WithMany()
    .HasForeignKey(x => x.ChatMessageId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessageSeen>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}