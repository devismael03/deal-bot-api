using DealBot.Authentication;
using System.ComponentModel.DataAnnotations;

namespace DealBot.Account
{
    public class UpdateProfileDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

    }
}
