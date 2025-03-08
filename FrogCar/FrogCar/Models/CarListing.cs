using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FrogCar.Models
{
    public class CarListing
    {
        public int Id { get; set; }

        [Required]
        public string Brand { get; set; } = string.Empty;

        [Required]
        public double EngineCapacity { get; set; } // Pojemność silnika w litrach

        [Required]
        public string FuelType { get; set; } = string.Empty; // Rodzaj paliwa (np. Benzyna, Diesel, Elektryczny)

        [Required]
        public int Seats { get; set; } // Liczba miejsc

        [Required]
        public string CarType { get; set; } = string.Empty; // Typ auta (SUV, Sedan, Hatchback)

        public List<string> Features { get; set; } = new(); // Lista dodatków

        public double Latitude { get; set; } // Szerokość geograficzna
        public double Longitude { get; set; } // Długość geograficzna

        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; } // Relacja z użytkownikiem
    }
}
