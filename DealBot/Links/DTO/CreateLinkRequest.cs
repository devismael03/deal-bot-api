using DealBot.Links.Models;
using System.ComponentModel.DataAnnotations;

namespace DealBot.Links.DTO
{
    public class CreateLinkRequest
    {
        [Required]
        public string Url { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public LinkTypeEnum LinkType { get; set; }

    }
}
