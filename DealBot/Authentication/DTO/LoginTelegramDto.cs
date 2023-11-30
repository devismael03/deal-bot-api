using System.ComponentModel.DataAnnotations;

namespace DealBot.Authentication.DTO
{
    public class LoginTelegramDto
    {
        [Required]
        public string Id { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Required]
        public string Hash { get; set; }

        public string? PhotoUrl { get; set; }

        public string? Username { get; set; }

        [Required]
        public string AuthDate { get; set; }

    }
}
