using familyTBotBack.TypedClass;
using familyTBotBack.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace familyTBotBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramAuthController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Profile> _profiles;

        public TelegramAuthController(MongoDbContext database)
        {
            _users = database.Users;
            _profiles = database.Profiles;
        }

        [AllowAnonymous]
        [HttpGet("authenticate")]
        public IActionResult telegramAuth()
        {
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = "/api/TelegramAuth/me"
                },
                OpenIdConnectDefaults.AuthenticationScheme);
        }


        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            
            long.TryParse(userIdClaim, out var userId);
            FilterDefinition<Profile> filter =
                Builders<Profile>.Filter.Eq(profile => profile.UserId, userId);
            var update = Builders<Profile>.Update.Set(profile => profile.AuthOnWeb, true);
            await _profiles.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            Profile? prof = await _profiles.Find(Builders<Profile>.Filter.Eq(Profile => Profile.UserId, userId)).FirstOrDefaultAsync();
            return Ok(prof);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        [Authorize]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _users
                .Find(Builders<User>.Filter.Empty)
                .ToListAsync();

            return Ok(users);
        }
    }

}
