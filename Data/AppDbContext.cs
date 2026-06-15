using Microsoft.EntityFrameworkCore;
using CampusBookProject.Models;

namespace CampusBookProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Book ilişkisi
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Owner)
                .WithMany()
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Book - Request ilişkisi
            modelBuilder.Entity<Request>()
                .HasOne(r => r.Book)
                .WithMany()
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Request ilişkisi
            modelBuilder.Entity<Request>()
                .HasOne(r => r.Requester)
                .WithMany()
                .HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index'ler (performans için)
            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Category);

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Status);

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.OwnerId);

            modelBuilder.Entity<Request>()
                .HasIndex(r => r.BookId);

            modelBuilder.Entity<Request>()
                .HasIndex(r => r.RequesterId);

            modelBuilder.Entity<Request>()
                .HasIndex(r => r.Status);
        }
    }
}