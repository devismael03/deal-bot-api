namespace DealBot.Links.DTO
{
    public class LinkResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string? Title { get; set; }
        public int? CurrentPrice { get; set; }
        public DateTime? LastPriceChange { get; set; }
    }
}
