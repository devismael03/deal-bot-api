using DealBot.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealBot.Account
{

    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager,ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [Authorize(Roles = UserRoles.CustomUser)]
        [HttpPatch("subscriptionModel")]
        public async Task<IActionResult> UpdateSubscriptionModel([FromBody] UpdateSubscriptionModelDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = this.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == "Id")?.Value;
            if(user == null)
            {
                return Unauthorized();
            }

            var userToUpdate = _userManager.Users.FirstOrDefault(u => u.Id == user);
            if(userToUpdate == null)
            {
                return NotFound();
            }
            var limits = new Dictionary<SubscriptionModelEnum, int>{
                { SubscriptionModelEnum.Basic,3},
                {SubscriptionModelEnum.Standart,5 },
                {SubscriptionModelEnum.Premium, 8 }
            };
            if(userToUpdate.SubscriptionModel > model.SubscriptionModel)
            {
                if(_context.Links.Where(link => link.User.Id == user).Count() > limits[model.SubscriptionModel])
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Please delete records to downgrade subscription plan!" });
                }
            }

            userToUpdate.SubscriptionModel = model.SubscriptionModel;

            var result = await _userManager.UpdateAsync(userToUpdate);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Update of subscription model failed!" });

            return Ok();

        }

        [Authorize(Roles = UserRoles.CustomUser)]
        [HttpGet("subscriptionModel")]
        public async Task<IActionResult> GetSubscriptionModel()
        {
            var userId = this.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == "Id")?.Value;
            var user = _userManager.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(new {SubscriptionModel=user.SubscriptionModel});
        }


        [Authorize(Roles = UserRoles.AdManager)]
        [HttpPut("admanager")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
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

            var userToUpdate = _userManager.Users.FirstOrDefault(u => u.Id == user);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            userToUpdate.FirstName = model.Name;
            userToUpdate.LastName = model.Surname;   

            var result = await _userManager.UpdateAsync(userToUpdate);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Update of user profile info failed!" });

            return Ok();

        }


        [Authorize(Roles = UserRoles.AdManager)]
        [HttpGet("admanager")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = this.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == "Id")?.Value;
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(new { Name = user.FirstName, Surname = user.LastName});
        }


        [Authorize(Roles = UserRoles.AdManager)]
        [HttpPatch("admanager/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
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

            var userToUpdate = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == user);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(userToUpdate, model.OldPassword, model.NewPassword);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Update of password failed!" });

            return Ok();

        }


    }
}
