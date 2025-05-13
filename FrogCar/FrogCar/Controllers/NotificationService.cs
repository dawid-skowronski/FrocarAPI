using FrogCar.Data;
using FrogCar.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrogCar.Controllers
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string title, string message, string type = "Info");
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(int userId, string title, string message, string type = "Info")
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }

}
