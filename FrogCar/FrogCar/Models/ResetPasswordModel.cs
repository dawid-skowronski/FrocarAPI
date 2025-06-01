using System.ComponentModel.DataAnnotations;

namespace FrogCar.Models
{
    public class ResetPasswordModel
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string NewPassword { get; set; }
    }

}
