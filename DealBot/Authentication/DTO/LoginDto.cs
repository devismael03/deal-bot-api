using System.ComponentModel.DataAnnotations;

namespace DealBot.Authentication.DTO
{
    public class LoginDto
    {
        [EmailAddress]
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
