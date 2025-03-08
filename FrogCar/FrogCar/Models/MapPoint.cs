using FrogCar.Models;
using System.Text.Json.Serialization;

public class MapPoint
{
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int UserId { get; set; }


    [JsonIgnore]
    public User? User { get; set; }

}
