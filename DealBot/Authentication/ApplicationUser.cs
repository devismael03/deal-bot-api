using DealBot.Advertisement.Models;
using DealBot.Links.Models;
using Microsoft.AspNetCore.Identity;

namespace DealBot.Authentication
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? TelegramId { get; set; }
        public SubscriptionModelEnum? SubscriptionModel { get; set; }

        public ICollection<Link> Links { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; }
        public ICollection<TargetKeyword> TargetKeywords { get; set; }
    }
}
