namespace FrogCar.Models
{
    public class Notification
    {

        public int NotificationId { get; set; }

        public int UserId { get; set; } 
        public User User { get; set; }

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
