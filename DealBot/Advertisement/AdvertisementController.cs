using DealBot.Advertisement.DTO;
using DealBot.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DealBot.Account
{

    [ApiController]
    [Authorize(Roles = UserRoles.AdManager)]
    [Route("api/[controller]")]
    public class AdvertisementController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdvertisementController(UserManager<ApplicationUser> userManager,ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("keyword")]
        public async Task<IActionResult> GetKeywords()
        {
            return Ok(_context.TargetKeywords.Select(keyword => new
            {
                Id = keyword.Id,
                Keyword = keyword.Keyword,
                UserCount = keyword.Users.Count
            }));
        }


        [HttpPost]
        public async Task<IActionResult> PostAdvertisement(AdvertisementRequest model)
        {
            var telegramIds = new List<string>();

            foreach (var keywordId in model.Audiences)
            {
                var targetKeyword = await _context.TargetKeywords.Include(keyword => keyword.Users).FirstOrDefaultAsync(k => k.Id == Guid.Parse(keywordId));

                if (targetKeyword != null)
                {
                    telegramIds.AddRange(targetKeyword.Users.Select(user => user.TelegramId)!);
                }
            }
            telegramIds = telegramIds.Distinct().ToList();

            using (HttpClient apiClient = new HttpClient())
            {
                var messageBodyBuilder = new StringBuilder();
                foreach (var telegramId in telegramIds)
                {
                    messageBodyBuilder.Append($"*{model.Title}*\n{model.Body}\n");
                    if (!string.IsNullOrEmpty(model.Url))
                    {
                        messageBodyBuilder.Append($"[Link]({model.Url}");
                    }
                    var body = new
                    {
                        chat_id = telegramId,
                        text = messageBodyBuilder.ToString(),
                        parse_mode = "Markdown"
                    };
                    apiClient.PostAsJsonAsync($"https://api.telegram.org/botTOKEN/sendMessage", body).GetAwaiter().GetResult();
                    messageBodyBuilder.Clear();
                }
            }

            return Ok();

        }
    }
}
