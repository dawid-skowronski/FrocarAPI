using Microsoft.EntityFrameworkCore;
using FrogCar.Data;
using FrogCar.Controllers;
using System;
using Xunit;

namespace FrogCar.Tests.Service
{
    public class NotificationServiceTests
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public NotificationServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options; 
            _context = new AppDbContext(options); 
            _notificationService = new NotificationService(_context);
        }

        [Fact]
        public async Task CreateNotificationAsync_CreatesNotification()
        {
            int userId = 1;
            string title = "Test Title";
            string message = "Test Message";

            await _notificationService.CreateNotificationAsync(userId, title, message, type: "Info");

            var notification = await _context.Notifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(userId, notification.UserId);
            Assert.Equal(message, notification.Message);
            Assert.False(notification.IsRead);
            Assert.Equal(DateTime.UtcNow, notification.CreatedAt, TimeSpan.FromSeconds(1));
        }
    }
}