namespace FrogCar.Models
{
    public class CarRentalReviewRequest
    {
        public int CarRentalId { get; set; }
        public int Rating { get; set; } 
        public string? Comment { get; set; }
    }

}
