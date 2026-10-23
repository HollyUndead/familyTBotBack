using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using familyTBotBack.Database;
using familyTBotBack.TypedClass;
using MongoDB.Driver;
using System.Net;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using CloudinaryDotNet;
using familyTBotBack.Midlleware;

namespace familyTBotBack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));
            var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!));
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudienc = builder.Configuration["Jwt:Audience"];

            var cfg = builder.Configuration.GetSection("Cloudinary");
            var cloudinary = new Cloudinary(new Account(cfg["CloudName"], cfg["ApiKey"], cfg["ApiSecret"]));
            cloudinary.Api.Secure = true;

            builder.Services.AddSingleton(cloudinary);
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudienc,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwtKey,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            }).AddCookie("External").AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = "External";
                //options.Events = new OpenIdConnectEvents { 
                //    OnRedirectToIdentityProvider = context =>
                //    {
                //        if (context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.Equals("/api/telegramauth/authenticate"))
                //        {
                //            context.Response.StatusCode = 401;
                //            context.Response.ContentType = "application/json";
                //            context.HandleResponse();
                //            return context.Response.WriteAsync("{\"Error\": \"User not authorized\"}");
                //        }
                //        return Task.CompletedTask;
                //    }
                //};
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
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.CallbackPath = builder.Configuration.GetConnectionString("RedirectPath");

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("phone");

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = ctx =>
                    {
                        // keep your existing /api check here if you still need it
                        Console.WriteLine($"OUT state len={ctx.ProtocolMessage.State?.Length}: {ctx.ProtocolMessage.State}");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = ctx =>
                    {
                        Console.WriteLine($"IN  state len={ctx.ProtocolMessage.State?.Length}: {ctx.ProtocolMessage.State}");
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = ctx =>
                    {
                        Console.WriteLine(ctx.Failure);
                        ctx.Response.StatusCode = 500;
                        ctx.HandleResponse();
                        return ctx.Response.WriteAsync(ctx.Failure?.Message ?? "failed");
                    },
                    OnUserInformationReceived = ctx => {
                        Console.WriteLine("USERINFO: " + ctx.User.RootElement.GetRawText());
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        foreach (var c in ctx.Principal!.Claims)
                            Console.WriteLine($"IDTOKEN CLAIM {c.Type} = {c.Value}");
                        return Task.CompletedTask;
                    },
                    OnTicketReceived = ctx =>
                    {
                        Console.WriteLine(ctx.Principal);
                        var id = ctx.Principal!.FindFirst("id")?.Value;
                        //if (long.TryParse(id, out _))
                        //{
                        //    ctx.Response.StatusCode = 400;
                        //    ctx.HandleResponse();
                        //    return Task.CompletedTask;
                        //}

                        var claims = new List<Claim>
                        {
                            new("Id", id!),
                            new("name", ctx.Principal.FindFirst("name")?.Value ?? "")
                        };

                        var jwt = new JwtSecurityToken(issuer: jwtIssuer, audience: jwtAudienc, claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: new SigningCredentials(jwtKey, SecurityAlgorithms.HmacSha256));

                        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
                        var frontend = builder.Configuration.GetConnectionString("CallbackFrontPathLocal");

                        ctx.Response.Redirect($"{frontend}#token={token}");
                        ctx.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
                //options.TokenValidationParameters.NameClaimType = "name";
                //options.TokenValidationParameters.RoleClaimType = "role";

            });
            builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(builder.Configuration.GetConnectionString("Mongo"))); 
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration.GetConnectionString("mydb")));
            builder.Services.AddSingleton<MongoDbContext>();
            //builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("telegram", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Telegram:TelegramApi"]!);
            });
            builder.Services.AddScoped<ImageMiddleware>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy => policy
                    .WithOrigins(
                        "https://hollyundead.github.io",
                        "https://hollyundead.github.io/familyTBotFront/tasks",
                        "https://wireless-kiln-departure.ngrok-free.dev")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
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
