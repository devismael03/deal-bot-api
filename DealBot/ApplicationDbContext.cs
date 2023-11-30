using DealBot.Advertisement.Models;
using DealBot.Authentication;
using DealBot.Links.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DealBot
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Link> Links { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<TargetKeyword> TargetKeywords { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

    }
}
