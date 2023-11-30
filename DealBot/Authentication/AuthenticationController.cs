using DealBot.Authentication.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Telegram.Bot.Extensions.LoginWidget;

namespace DealBot.Authentication
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthenticationController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim("Id",user.Id),
                    new Claim("FirstName", user.FirstName),
                    new Claim("LastName", user.LastName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim("role", userRole));
                }

                var token = GenerateJwt(authClaims);
                var refreshToken = await GenerateRefreshToken(user, DateTime.UtcNow.AddMinutes(2));

                if (refreshToken == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "User Login failed!" });
                }

                return Ok(new AuthenticationResponse
                {
                    AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken.Token
                });
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new {Message = "User already exists!"});

            ApplicationUser user = new ApplicationUser()
            {
                FirstName = model.Name,
                LastName = model.Surname,
                Email = model.Email,
                UserName = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
            };


            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new {Message = "User creation failed!"});

            if (!await _roleManager.RoleExistsAsync(UserRoles.AdManager))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.AdManager));

            if (await _roleManager.RoleExistsAsync(UserRoles.AdManager))
            {
                await _userManager.AddToRoleAsync(user, UserRoles.AdManager);
            }


            return Ok(new { Message = "User created successfully!" });
        }

        [HttpPost]
        [Route("logintg")]
        public async Task<IActionResult> LoginTelegram([FromBody] LoginTelegramDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var botKey = "1944346895:AAHAsD_y3SVKL3hx79hD6B-kC_U1MJT2AwA";

            var info = new Dictionary<string, string>
            {
                {"auth_date", model.AuthDate},
                {"id", model.Id},
                {"hash", model.Hash}
            };

            if(model.PhotoUrl != null)
            {
                info.Add("photo_url", model.PhotoUrl);
            }

            if (model.FirstName != null)
            {
                info.Add("first_name", model.FirstName);
            }

            if (model.LastName != null)
            {
                info.Add("last_name", model.LastName);
            }

            if (model.Username != null)
            {
                info.Add("username", model.Username);
            }
            var dataString = CombineString(info);
            var computedHash = HashHMAC(dataString);

            var widget = new LoginWidget(botKey);
            if (model.Hash == computedHash.ToLower())
            {
                var existingUser = _userManager.Users.FirstOrDefault(u => u.TelegramId == model.Id);
                if(existingUser == null)
                {
                    ApplicationUser user = new ApplicationUser()
                    {
                        FirstName = model.FirstName ?? "",
                        LastName = model.LastName ?? "",
                        UserName = model.Username ?? "",
                        TelegramId = model.Id,
                        SubscriptionModel = SubscriptionModelEnum.Basic,
                        SecurityStamp = Guid.NewGuid().ToString(),
                    };


                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "User creation failed!" });

                    if (!await _roleManager.RoleExistsAsync(UserRoles.CustomUser))
                        await _roleManager.CreateAsync(new IdentityRole(UserRoles.CustomUser));

                    if (await _roleManager.RoleExistsAsync(UserRoles.CustomUser))
                    {
                        await _userManager.AddToRoleAsync(user, UserRoles.CustomUser);
                    }
                    existingUser = user;
                }
                var authClaims = new List<Claim>
                {
                    new Claim("Id", existingUser.Id),
                    new Claim("FirstName", existingUser.FirstName),
                    new Claim("LastName", existingUser.LastName),
                    new Claim("SubscriptionModel", existingUser.SubscriptionModel.ToString()!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                authClaims.Add(new Claim("role", UserRoles.CustomUser));

                var token = GenerateJwt(authClaims);
                var refreshToken = await GenerateRefreshToken(existingUser, DateTime.UtcNow.AddMinutes(2));

                if(refreshToken == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "User Login failed!" });
                }

                return Ok(new AuthenticationResponse
                {
                    AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken.Token
                });
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        [Route("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid payload");
            }

            var res = await VerifyToken(model);

            if (res == null)
            {
                return BadRequest("Invalid tokens");
            }

            return Ok(res);


        }


        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout(LogoutRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid payload!");
            }

            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mysupersecretdealbotkey"))
                };

                var principal = jwtTokenHandler.ValidateToken(model.AccessToken, tokenValidationParameters, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                    if (result == false)
                    {
                        return BadRequest();
                    }

                    var userId = principal.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

                    var storedRefreshToken = await _context.RefreshTokens.Include(token => token.User).FirstOrDefaultAsync(token => token.Token == model.RefreshToken && token.UserId == userId);
                    if (storedRefreshToken is null)
                    {
                        return BadRequest();
                    }

                    _context.RefreshTokens.Remove(storedRefreshToken);
                    await _context.SaveChangesAsync();

                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception)
            {

                return BadRequest("Logout failed");
            }

        }
        private string HashHMAC(string message)
        {
            using var hasher = SHA256.Create();
            var keyBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes("key-secret"));

            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hash = new HMACSHA256(keyBytes);
            var computedHash = hash.ComputeHash(messageBytes);
            return Convert.ToHexString(computedHash);
        }

        private string CombineString(IReadOnlyDictionary<string, string> meta)
        {
            var builder = new StringBuilder();

            TryAppend("auth_date");
            TryAppend("first_name");
            TryAppend("id");
            TryAppend("last_name");
            TryAppend("photo_url");
            TryAppend("username");

            builder.Remove(builder.Length - 1, 1);
            return builder.ToString();

            void TryAppend(string key)
            {
                if (meta.ContainsKey(key))
                    builder.Append($"{key}={meta[key]}\n");
            }
        }

        private JwtSecurityToken GenerateJwt(List<Claim> authClaims) 
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mysupersecretdealbotkey"));

            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(2),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        private async Task<RefreshToken?> GenerateRefreshToken(ApplicationUser user, DateTime expiryDate)
        {
            try
            {
                var randomNumber = new byte[32];
                var token = String.Empty;
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomNumber);
                    token = Convert.ToBase64String(randomNumber);
                }

                var refreshToken = new RefreshToken()
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    ExpiryDate = expiryDate, 
                    User = user
                };

                await _context.RefreshTokens.AddAsync(refreshToken);
                await _context.SaveChangesAsync();
                return refreshToken;

            }
            catch (Exception)
            {
                return null;
            }
           
        }

        private async Task<AuthenticationResponse?> VerifyToken(RefreshTokenRequest model)
        {
            Console.WriteLine("STARTED VERIFYING");

            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mysupersecretdealbotkey"))
                };

                var principal = jwtTokenHandler.ValidateToken(model.AccessToken, tokenValidationParameters, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                    if (result == false)
                    {
                        Console.WriteLine("HEADER ALG UNSUCCESSFUL");

                        return null;
                    }

                    //var utcExpiryDate = long.Parse(principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)!.Value);
                    //var expDate = UnixTimeStampToDateTime(utcExpiryDate);

                    //if (expDate > DateTime.UtcNow)
                    //{
                    //    return null;
                    //}

                    var userId = principal.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

                    var storedRefreshToken = await _context.RefreshTokens.Include(token =>token.User).FirstOrDefaultAsync(token => token.Token == model.RefreshToken && token.UserId == userId);
                    if (storedRefreshToken is null)
                    {
                        Console.WriteLine("DB REFRESH NOT FOUND ERROR");

                        return null;
                    }

                    if(DateTime.UtcNow > storedRefreshToken.ExpiryDate)
                    {
                        Console.WriteLine("REFRESH EXPIRED DELETING RECORD ERROR");

                        _context.RefreshTokens.Remove(storedRefreshToken);
                        await _context.SaveChangesAsync();
                        return null;
                    }

                     
                    var authClaims = new List<Claim>
                    {
                        new Claim("Id", storedRefreshToken.User.Id),
                        new Claim("FirstName", storedRefreshToken.User.FirstName),
                        new Claim("LastName", storedRefreshToken.User.LastName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                    var userRoles = await _userManager.GetRolesAsync(storedRefreshToken.User);

                    foreach(var role in userRoles)
                    {
                        authClaims.Add(new Claim("role",role));
                    }

                    if (userRoles.Contains(UserRoles.CustomUser))
                    {
                        authClaims.Add(new Claim("SubscriptionModel", storedRefreshToken.User.SubscriptionModel.ToString()!));
                    }

                    var jwtToken = GenerateJwt(authClaims);
                    var refreshToken = await GenerateRefreshToken(storedRefreshToken.User,storedRefreshToken.ExpiryDate);

                    if(refreshToken == null)
                    {
                        Console.WriteLine("REFRESH CANT BE GENERATED");

                        return null;
                    }

                    Console.WriteLine("REMOVING REFRESH FROM DB TO CREATE NEW ONE");

                    _context.RefreshTokens.Remove(storedRefreshToken);
                    await _context.SaveChangesAsync();


                    return new AuthenticationResponse
                    {
                        AccessToken = jwtTokenHandler.WriteToken(jwtToken),
                        RefreshToken = refreshToken.Token
                    };
                }
                else
                {
                    Console.WriteLine("FIRST CHECK UNSUSCCESSFUL");

                    return null;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("TRY CATCH ERROR");
                return null;
            }
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

    }
}