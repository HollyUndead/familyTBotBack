using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using familyTBotBack.Database;
using familyTBotBack.TypedClass;
using MongoDB.Driver;
using System.Net;

namespace familyTBotBack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }).AddCookie().AddOpenIdConnect(options =>
            {
                options.Authority = builder.Configuration.GetConnectionString("Authority");
                options.MetadataAddress = builder.Configuration.GetConnectionString("MetadataAddress");
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                options.StateDataFormat = new TelegramStateDataFormat();
                options.ClientId = builder.Configuration["Telegram:ClientID"];
                options.ClientSecret = builder.Configuration["Telegram:ClientSecret"];

                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = false;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                options.CallbackPath = builder.Configuration.GetConnectionString("RedirectPath");

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("phone");

                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";

            });

            builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(builder.Configuration.GetConnectionString("Mongo"))); 
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration.GetConnectionString("mydb")));
            builder.Services.AddSingleton<MongoDbContext>();
            builder.Services.AddHttpClient();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy => policy
                    .WithOrigins(
                        "https://hollyundead.github.io",
                        "https://wireless-kiln-departure.ngrok-free.dev")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseCors("Frontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
