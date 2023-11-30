using DealBot.Authentication;

namespace DealBot.Links.Models
{
    public class Link
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string? Title { get; set; }
        public LinkTypeEnum LinkType { get; set; }
        public ProviderEnum Provider { get; set; }
        public int? CurrentPrice { get; set; }
        public DateTime? LastPriceChange { get; set; }
        public ApplicationUser User { get; set; }
    }
}
