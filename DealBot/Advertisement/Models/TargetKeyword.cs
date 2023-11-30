using DealBot.Authentication;

namespace DealBot.Advertisement.Models
{
    public class TargetKeyword
    {
        public Guid Id { get; set; }
        public string Keyword { get; set; }
        public ICollection<ApplicationUser> Users { get; set; }
    }
}
