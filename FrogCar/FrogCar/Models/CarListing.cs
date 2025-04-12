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
        public double EngineCapacity { get; set; } 

        [Required]
        public string FuelType { get; set; } = string.Empty; 

        [Required]
        public int Seats { get; set; } 

        [Required]
        public string CarType { get; set; } = string.Empty; 

        public List<string> Features { get; set; } = new(); 

        public double Latitude { get; set; } 
        public double Longitude { get; set; } 

        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }
        public bool IsAvailable { get; set; } = true;

        public bool IsApproved { get; set; } = false;


        [Required]
        public decimal RentalPricePerDay { get; set; }

    }
}
