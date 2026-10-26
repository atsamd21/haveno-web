using Haveno.Api.Helpers;
using Haveno.Extensions;
using Haveno.Processes;
using Merkato.Models;
using Merkato.Tor;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration.GetRequiredSection("AppSettings"));

//TorManager torManager = new();
//await torManager.EnsureTorInstalledAsync();
//torManager.StartTor();

var appsettings = builder.Configuration.GetRequiredSection("AppSettings").Get<AppSettings>()!;
HavenoDaemon havenoDaemon = new(appsettings.HavenoDaemonPath!, appsettings.HavenoDaemonArguments!);

await havenoDaemon.StartAsync();

builder.Services.AddRazorPages();
builder.Services.AddHavenoServices();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en-GB"),
        new CultureInfo("fr-FR"),
    };

    options.DefaultRequestCulture = new RequestCulture("en-GB");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddMvc().AddViewLocalization();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

// Move this
// Also breaks if user for some reason chooses to navigate to the settings page before any other page has loaded
app.Use(async (context, next) =>
{
    if (!context.Request.Cookies.ContainsKey("currency"))
    {
        context.Response.Cookies.Append(
            "currency",
            CurrencyHelper.FiatCurrenciesDictionary.First().Key,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
    }

    await next();
});

app.Run();
