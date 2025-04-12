namespace FrogCar.Models
{
    public class CarRental
{
    public int CarRentalId { get; set; } // Unikalny identyfikator wypożyczenia
    public int CarListingId { get; set; } // Id ogłoszenia, do którego należy wypożyczenie
    public CarListing CarListing { get; set; } // Powiązanie z ogłoszeniem

    public int UserId { get; set; } // Id użytkownika, który wypożyczył samochód
    public User User { get; set; } // Powiązanie z użytkownikiem

    public DateTime RentalStartDate { get; set; } // Data rozpoczęcia wypożyczenia
    public DateTime RentalEndDate { get; set; } // Data zakończenia wypożyczenia

    public decimal RentalPrice { get; set; } // Całkowita cena wypożyczenia
    public string RentalStatus { get; set; } // Status wypożyczenia (np. "aktywny", "zakończony", "anulowany")
}

}
