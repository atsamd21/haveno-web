using Haveno.Api.Helpers;
using Haveno.Extensions;
using Haveno.Processes;
using Merkato.Middleware;
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
builder.Services.AddHavenoServices(
    new HavenoOptions 
    { 
        Host = $"http://127.0.0.1:{appsettings.HavenoDaemonArguments!.ApiPort}", 
        Password = appsettings.HavenoDaemonArguments!.ApiPassword!
    });

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

app.UsePreferences();

app.Run();
