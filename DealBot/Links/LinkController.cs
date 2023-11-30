using DealBot.Authentication;
using DealBot.Links.DTO;
using DealBot.Links.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace DealBot.Links
{

    [ApiController]
    [Authorize(Roles = UserRoles.CustomUser)]
    [Route("api/[controller]")]
    public class LinkController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;


        public LinkController(UserManager<ApplicationUser> userManager,ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }


       [HttpGet]
       public async Task<IActionResult> GetLinks([FromQuery] LinkTypeEnum type)
       {
            var user = this.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == "Id")?.Value;
            if (user == null)
            {
                return Unauthorized();
            }

            var links = _context.Links.Where(link => link.LinkType == type && link.User.Id == user)
                                      .Select(link => new LinkResponse {
                                        Id = link.Id,
                                        Url = link.Url,
                                        Title = link.Title,
                                        CurrentPrice = link.CurrentPrice,
                                        LastPriceChange = link.LastPriceChange
                                      });

            return Ok(await links.ToListAsync());
       }

        [HttpPost]
        public async Task<IActionResult> AddLink([FromBody] CreateLinkRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var user = this.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == "Id")?.Value;
            if (user == null)
            {
                return Unauthorized();
            }

            var linkCount = _context.Links.Where(link => link.User.Id == user).Count();
            var userSubscriptionType = _userManager.Users.Where(u => u.Id == user).Select(u => u.SubscriptionModel).FirstOrDefault();

            var canProceedInsertion = true;

            switch (userSubscriptionType)
            {
                case SubscriptionModelEnum.Basic:
                    if(linkCount >= 3)
                    {
                        canProceedInsertion = false;
                    }
                    break;
                case SubscriptionModelEnum.Standart:
                    if(linkCount >= 5)
                    {
                        canProceedInsertion = false;
                    }
                    break;
                case SubscriptionModelEnum.Premium:
                    if(linkCount >= 8)
                    {
                        canProceedInsertion = false;
                    }
                    break;
            }

            if (!canProceedInsertion)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Your link insertion limit is full!" });
            }

            var link = new Link
            {
                Id = Guid.NewGuid(),
                Url = model.Url,
                Title = model.Title,
                LinkType = model.LinkType,
                User = _userManager.Users.Single(u => u.Id == user)
            };

            Uri uri = new Uri(model.Url);

            link.Provider = uri.Host switch
            {
                "www.turbo.az" => ProviderEnum.TurboAz,
                "turbo.az" => ProviderEnum.TurboAz,
                "www.bina.az" => ProviderEnum.BinaAz,
                "bina.az" => ProviderEnum.BinaAz,
                "ww.tap.az" => ProviderEnum.TapAz,
                "tap.az" => ProviderEnum.TapAz,
                _ => throw new ArgumentException("Please enter correct url type!")
            };
            
            if(link.LinkType == LinkTypeEnum.PriceChecking)
            {
                link.LastPriceChange = DateTime.UtcNow;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;

                using (WebClient client = new WebClient())
                {
                    //client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " + "Windows NT 5.2; .NET CLR 1.0.3705;)");
                    try
                    {
                        string htmlCode = client.DownloadString(model.Url);

                        HtmlDocument doc = new HtmlDocument();

                        doc.LoadHtml(htmlCode);

                        switch (link.Provider)
                        {
                            case ProviderEnum.TurboAz:
                                var turboAzString = doc.DocumentNode.SelectSingleNode("//div[@class='product']//div[@class='product-price']").InnerText;
                                //add parsing for avtosalon type also
                                var turboAzParse = Int32.TryParse(turboAzString.Substring(0, turboAzString.LastIndexOf(" ")).Replace(" ", String.Empty), out int val);
                                if (turboAzParse)
                                {
                                    link.CurrentPrice = val;
                                }
                                break;
                            case ProviderEnum.BinaAz:
                                var binaAzParse = Int32.TryParse(doc.DocumentNode.SelectSingleNode("//div[@class='price_header']//span[@class='price-val']").InnerText.Replace(" ", String.Empty), out int val2);
                                if (binaAzParse)
                                {
                                    link.CurrentPrice = val2;
                                }
                                break;
                            case ProviderEnum.TapAz:
                                var tapAzParse = Int32.TryParse(doc.DocumentNode.SelectSingleNode("//div[@class='lot-header fixed-product-info']//span[@class='price-val']").InnerText.Replace(" ", String.Empty), out int val3);
                                if (tapAzParse)
                                {
                                    link.CurrentPrice = val3;
                                }
                                break;
                            default:
                                break;
                        }

                    }
                    catch (Exception)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error occured during link insertion!" });
                    }
                }
            }
            else
            {
                var currentUser = _userManager.Users.Include(u=> u.TargetKeywords).Where(u => u.Id == user).FirstOrDefault();
                switch (link.Provider)
                {
                    case ProviderEnum.TurboAz:
                        currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "Turbo.az").FirstOrDefault()!);
                        if (uri.Query.Contains("q%5Bloan%5D=1"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "Kredit avtomobil").FirstOrDefault()!);
                        }

                        if (uri.Query.Contains("q%5Bfuel_type%5D%5B%5D=4"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "Elektro mühərrik").FirstOrDefault()!);
                        }

                        if (uri.Query.Contains("q%5Bfuel_type%5D%5B%5D=5"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "Hibrid mühərrik").FirstOrDefault()!);
                        }
                        break;
                    case ProviderEnum.BinaAz:
                        currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "Bina.az").FirstOrDefault()!);

                        if (uri.Query.Contains("has_repair=false"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "Təmirsiz evlər").FirstOrDefault()!);
                        }
                        
                        if (uri.Query.Contains("room_ids%5B%5D=1") || uri.OriginalString.Contains("1-otaqli"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "1 otaqlı").FirstOrDefault()!);
                        }

                        if (uri.Query.Contains("room_ids%5B%5D=2") || uri.OriginalString.Contains("2-otaqli"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "2 otaqlı").FirstOrDefault()!);
                        }

                        if (uri.Query.Contains("room_ids%5B%5D=3") || uri.OriginalString.Contains("3-otaqli"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "3 otaqlı").FirstOrDefault()!);
                        }

                        if (uri.Query.Contains("room_ids%5B%5D=4")  || uri.OriginalString.Contains("4-otaqli"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "4 otaqlı").FirstOrDefault()!);
                        }

                        if (uri.Query.Contains("room_ids%5B%5D=5%2B") || uri.OriginalString.Contains("5-otaqli"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "5 otaqlı və daha çox").FirstOrDefault()!);
                        }

                        if (uri.Query.Contains("location_ids%5B%5D=34") || uri.OriginalString.Contains("elmler-akademiyasi"))
                        {
                            currentUser.TargetKeywords.Add(_context.TargetKeywords.Where(keyword => keyword.Keyword == "Elmlər akademiyası m.").FirstOrDefault()!);
                        }

                        break;
                    default:
                        break;
                }
            }
            try
            {
                await _context.Links.AddAsync(link);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error occured during link insertion!" });
            }

            return Ok();

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = this.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == "Id")?.Value;
            if (user == null)
            {
                return Unauthorized();
            }

            var linkToDelete = _context.Links.Where(link => link.Id == id && link.User.Id == user).FirstOrDefault();
            if(linkToDelete == null)
            {
                return NotFound();
            }

            try
            {
                _context.Remove(linkToDelete);
                await _context.SaveChangesAsync();
            }
            catch(Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error occured during link removal!" });
            }

            return Ok();
        }
    }
}
