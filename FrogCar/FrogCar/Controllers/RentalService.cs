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

            
            var rentals = await _context.CarRentals
                .Include(r => r.CarListing) 
                .Where(r => r.RentalEndDate < now && r.RentalStatus != "Zakończone")
                .ToListAsync();

            foreach (var rental in rentals)
            {
                rental.RentalStatus = "Zakończone"; 

                if (rental.CarListing != null)
                {
                    rental.CarListing.IsAvailable = true;
                }

                
                var message = $"Twoje wypożyczenie samochodu o ID {rental.CarRentalId} zostało zakończone.";
                await _notificationService.CreateNotificationAsync(rental.UserId, "Wypożyczenie zakończone", message);
            }
            await _context.SaveChangesAsync();
        }
    }
}
