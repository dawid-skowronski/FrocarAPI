using System.ComponentModel.DataAnnotations;

namespace FrogCar.Models
{
    public class ChangeUsernameModel
    {
        [Required]
        public string NewUsername { get; set; }
    }

}
