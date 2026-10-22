using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace familyTBotBack.TypedClass
{
    public class TelegramOptions
    {
        public const string SectionName = "Telegram";
        public string ClientID { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string RedirectUrl { get; set; } = "";
    }

    public class TelegramTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = "";
    }

    //public class TelegramUser
    //{
    //    [JsonPropertyName('')]
    //}
}
