using Aes256CryptoLib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using WouldYouRather.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on PORT environment variable when present
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Read encrypted settings from environment (fail-fast if missing)
var encAccess = Environment.GetEnvironmentVariable("ChannelAccessToken") ?? builder.Configuration["ChannelAccessToken"];
var encSecret = Environment.GetEnvironmentVariable("ChannelSecret") ?? builder.Configuration["ChannelSecret"];
if (string.IsNullOrWhiteSpace(encAccess) || string.IsNullOrWhiteSpace(encSecret))
{
    Console.Error.WriteLine("Missing required encrypted environment variables: ChannelAccessToKen or ChannelSecret.");
    // Exit with non-zero to prevent service from starting with invalid config
    Environment.Exit(1);
}

var crypto = new Aes256CryptoService();
string accessPlain;
string secretPlain;
try
{
    accessPlain = crypto.Decrypt(encAccess);
    secretPlain = crypto.Decrypt(encSecret);
    Console.WriteLine($"accessPlain:{accessPlain[..5]}");
    Console.WriteLine($"secretPlain:{secretPlain[..5]}");
}
catch (Exception ex)
{
    // Do not log secrets; only log high-level error
    Console.Error.WriteLine($"Failed to decrypt LineBot settings: {ex.Message}");
    Environment.Exit(1);
    throw; // unreachable, but keeps compiler happy
}

// Bind decrypted settings into IOptions<LineBotSetting>
builder.Services.Configure<LineBotSetting>(opts =>
{
    opts.ChannelAccessToken = accessPlain;
    opts.ChannelSecret = secretPlain;
});

var app = builder.Build();

app.MapGet("/health", () => Results.Json(new
{
    status = "OK",
    utc = DateTime.UtcNow.ToString("o")
}, new System.Text.Json.JsonSerializerOptions
{
    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
}));

app.Run();