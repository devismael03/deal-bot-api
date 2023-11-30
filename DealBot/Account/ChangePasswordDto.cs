using DealBot.Authentication;
using System.ComponentModel.DataAnnotations;

namespace DealBot.Account
{
    public class ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }

    }
}
