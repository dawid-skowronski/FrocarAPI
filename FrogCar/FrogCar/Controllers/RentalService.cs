using Microsoft.EntityFrameworkCore;
using FrogCar.Data;

namespace FrogCar.Controllers
{
    public interface IRentalService
    {
        Task UpdateEndedRentalsAsync();
    }

    public class RentalService : IRentalService
    {
        private readonly AppDbContext _context;

        public RentalService(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpdateEndedRentalsAsync()
        {
            var now = DateTime.Now;

            var rentals = await _context.CarRentals
                .Include(r => r.CarListing) 
                .Where(r => r.RentalEndDate < now && r.RentalStatus != "Ended")
                .ToListAsync();

            foreach (var rental in rentals)
            {
                rental.RentalStatus = "Ended";
                if (rental.CarListing != null)
                {
                    rental.CarListing.IsAvailable = true; 
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}
