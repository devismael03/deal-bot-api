using System.ComponentModel.DataAnnotations;

namespace DealBot.Authentication.DTO
{
    public class LogoutRequest
    {
        [Required]
        public string AccessToken { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}
