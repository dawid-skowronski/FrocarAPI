namespace FrogCar.Models
{
    public class CarRental
{
    public int CarRentalId { get; set; } 
    public int CarListingId { get; set; } 
    public CarListing CarListing { get; set; } 

<<<<<<< HEAD
    public int UserId { get; set; } 
=======
    public int UserId { get; set; } // Id użytkownika, który wypożyczył samochód
>>>>>>> ed449c728b1fbb4ad275323d0623767cd278a676
    public User User { get; set; } 

    public DateTime RentalStartDate { get; set; } 
    public DateTime RentalEndDate { get; set; } 

    public decimal RentalPrice { get; set; } 
    public string RentalStatus { get; set; } 
}

}
