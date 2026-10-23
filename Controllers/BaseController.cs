using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using familyTBotBack.Database;
using familyTBotBack.Midlleware;
using familyTBotBack.TypedClass;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace familyTBotBack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramAuthController : ControllerBase
    {
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
    }
    [ApiController]
    [Route("api/[controller]")]
    public class UserBaseController : ControllerBase
    {
        private readonly IHttpClientFactory _factory;
        private readonly string _token;
        private readonly ImageMiddleware _images;

        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Profile> _profiles;
        private readonly IConfiguration _config;

        public UserBaseController(MongoDbContext database, ImageMiddleware images, IHttpClientFactory factory,IConfiguration config)
        {
            _users = database.Users;
            _profiles = database.Profiles;
            _images = images;
            _factory = factory;
            _config = config;
            _token = $"{config["Telegram:BotID"]}:{config["Telegram:BotSecret"]}" ?? throw new InvalidOperationException("Telegram:BotToken is missing");
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var userId = long.Parse(User.FindFirst("Id")!.Value);
            FilterDefinition<Profile> filter =
                Builders<Profile>.Filter.Eq(profile => profile.UserId, userId);
            Profile? prof = await _profiles.Find(Builders<Profile>.Filter.Eq(Profile => Profile.UserId, userId)).FirstOrDefaultAsync();

            if(prof.avatarUrl != null)
            {
                return Ok(prof);
            }
            var http = _factory.CreateClient("telegram");
            var baseUrl = $"{_config["Telegram:TelegramApi"]}bot{_token}";
            var photoResp = await http.PostAsJsonAsync(
                 $"{baseUrl}/getUserProfilePhotos", new { user_id = userId }, CancellationToken.None);

            var photos = await photoResp.Content.ReadFromJsonAsync<TgResponse<TgUserPhotos>>(CancellationToken.None);

            if (photos is not { Ok: true } || photos.Result is null) return StatusCode(502, "Telegram request failed");
            if (photos.Result.TotalCount == 0) return NotFound("User has no profile photo");
            var fileId = photos.Result.Photos[0].Last().FileId;

            var fileResp = await http.GetFromJsonAsync<TgResponse<TgFile>>($"{baseUrl}/getFile?file_id={Uri.EscapeDataString(fileId)}", CancellationToken.None);

            if (fileResp is not { Ok: true } || fileResp.Result?.FilePath is null) return StatusCode(502, "Could not get file path from Telegram");

            var bytes = await http.GetByteArrayAsync($"{_config["Telegram:TelegramApi"]}file/bot{_token}/{fileResp.Result.FilePath}", CancellationToken.None);
            await using var ms = new MemoryStream(bytes);
            var formFile = new FormFile(ms, 0, bytes.Length, "file", "avatar.jpeg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };
            var url = "";
            try
            {
                url = await _images.PostImageFromFile(formFile, "telegram-avatars");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }

            var update = Builders<Profile>.Update.Set(a => a.avatarUrl, url);

            var result = await _profiles.UpdateOneAsync(filter, update);

            Profile? updatedProf = await _profiles.Find(Builders<Profile>.Filter.Eq(Profile => Profile.UserId, userId)).FirstOrDefaultAsync();

            return Ok(updatedProf);
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

    [ApiController]
    [Route("/api/{controller}")]
    public class ImagesController : ControllerBase {
        private readonly ImageMiddleware _images;

        public ImagesController(ImageMiddleware images) => _images = images;

        [HttpPost("postimage")]
        public async Task<IActionResult> PostImage(IFormFile file)
        {
            try
            {
                var url = await _images.PostImageFromFile(file);
                return Ok(new { url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    } 

}
