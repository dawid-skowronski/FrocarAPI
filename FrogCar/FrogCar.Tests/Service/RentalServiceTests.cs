using Microsoft.EntityFrameworkCore;
using FrogCar.Data;
using FrogCar.Controllers;
using FrogCar.Models;
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace FrogCar.Tests.Service
{
    public class RentalServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly RentalService _rentalService;

        public RentalServiceTests()
        {
            // Konfiguracja bazy w pamięci
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _notificationServiceMock = new Mock<INotificationService>();
            _rentalService = new RentalService(_context, _notificationServiceMock.Object);
        }

        [Fact]
        public async Task UpdateEndedRentalsAsync_UpdatesRentalStatusAndNotifies()
        {
            // Arrange
            var now = DateTime.Now;
            var rental = new CarRental
            {
                CarRentalId = 1,
                UserId = 1,
                RentalEndDate = now.AddHours(-1),
                RentalStatus = "Aktywne",
                CarListing = new CarListing { IsAvailable = false }
            };

            // Dodaj dane do bazy w pamięci
            _context.CarRentals.Add(rental);
            await _context.SaveChangesAsync();

            // Act
            await _rentalService.UpdateEndedRentalsAsync();

            // Assert
            Assert.Equal("Zakończone", rental.RentalStatus);
            Assert.True(rental.CarListing.IsAvailable);
            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(
                1, "Wypożyczenie zakończone", $"Twoje wypożyczenie samochodu o ID 1 zostało zakończone.", "Info"), Times.Once());
            var updatedRental = await _context.CarRentals.FindAsync(1);
            Assert.Equal("Zakończone", updatedRental.RentalStatus);
        }
    }
}