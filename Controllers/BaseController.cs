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
            var userId = long.Parse(User.FindFirst("Id")!.Value);

            FilterDefinition<Profile> filter =
                Builders<Profile>.Filter.Eq(profile => profile.UserId, userId);
            Profile? prof = await _profiles.Find(Builders<Profile>.Filter.Eq(Profile => Profile.UserId, userId)).FirstOrDefaultAsync();
            return Ok(prof);
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
