namespace FrogCar.Models
{
    public class CarRentalRequest
    {
        public int CarListingId { get; set; } 
        public DateTime RentalStartDate { get; set; } 
        public DateTime RentalEndDate { get; set; }
    }

}
