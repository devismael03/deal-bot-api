using System.ComponentModel.DataAnnotations;

namespace DealBot.Authentication.DTO
{
    public class RegisterDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        [EmailAddress]
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
