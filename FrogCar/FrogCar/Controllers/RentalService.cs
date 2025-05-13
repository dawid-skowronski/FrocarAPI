using Microsoft.EntityFrameworkCore;
using FrogCar.Data;
using System.Threading.Tasks;

namespace FrogCar.Controllers
{
    public interface IRentalService
    {
        Task UpdateEndedRentalsAsync();
    }

    public class RentalService : IRentalService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public RentalService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task UpdateEndedRentalsAsync()
        {
            var now = DateTime.Now;

            // Pobierz wypożyczenia, które zakończyły się, ale jeszcze nie zostały oznaczone jako "Ended"
            var rentals = await _context.CarRentals
                .Include(r => r.CarListing) // Dołącz dane samochodu, aby móc zmienić dostępność
                .Where(r => r.RentalEndDate < now && r.RentalStatus != "Ended")
                .ToListAsync();

            foreach (var rental in rentals)
            {
                rental.RentalStatus = "Ended"; // Zmiana statusu wypożyczenia na "Ended"

                // Ustawienie samochodu jako dostępnego
                if (rental.CarListing != null)
                {
                    rental.CarListing.IsAvailable = true;
                }

                // Wyślij powiadomienie do użytkownika
                var message = $"Twoje wypożyczenie samochodu o ID {rental.CarRentalId} zostało zakończone.";
                await _notificationService.CreateNotificationAsync(rental.UserId, "Wypożyczenie zakończone", message);
            }

            // Zapisz zmiany w bazie danych
            await _context.SaveChangesAsync();
        }
    }
}
