namespace FrogCar.Models
{
    public class CarRental
{
    public int CarRentalId { get; set; } 
    public int CarListingId { get; set; } 
    public CarListing CarListing { get; set; } 

    public int UserId { get; set; } 
    public User User { get; set; } 

    public DateTime RentalStartDate { get; set; } 
    public DateTime RentalEndDate { get; set; } 

    public decimal RentalPrice { get; set; } 
    public string RentalStatus { get; set; } 
}

}
