using FrogCar.Models;

public class CarRentalReview
{
    public int ReviewId { get; set; } 

    public int CarRentalId { get; set; }
    public CarRental CarRental { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
