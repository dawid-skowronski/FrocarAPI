using Microsoft.EntityFrameworkCore;
using FrogCar.Models;

namespace FrogCar.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CarListing> CarListing { get; set; }

        public DbSet<CarRental> CarRentals { get; set; }
        public DbSet<CarRentalReview> CarRentalReviews { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<CarListing>()
                .HasOne(cl => cl.User)
                .WithMany()
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarRentalReview>()
        .HasOne(r => r.User)
        .WithMany()
        .HasForeignKey(r => r.UserId)
        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CarRentalReview>()
    .HasOne(r => r.CarRental)
    .WithMany()
    .HasForeignKey(r => r.CarRentalId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarRentalReview>()
    .HasKey(r => r.ReviewId); 

        }

    }
}
