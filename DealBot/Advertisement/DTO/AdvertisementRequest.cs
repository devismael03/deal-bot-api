namespace DealBot.Advertisement.DTO
{
    public class AdvertisementRequest
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string Url { get; set; } = string.Empty;
        public List<string> Audiences { get; set; }
    }
}
