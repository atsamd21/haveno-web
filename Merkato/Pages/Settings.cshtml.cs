using Haveno.Api.Helpers;
using Haveno.Api.Models;
using Haveno.Api.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Merkato.Pages;

public class SettingsModel : PageModel
{
    private readonly ILogger<SettingsModel> _logger;
    private readonly IXmrNodeService _xmrNodeService;
    private readonly IVersionService _versionService;
    private readonly IPriceService _priceService;
    private readonly IOptions<RequestLocalizationOptions> _localizationOptions;

    public string HavenoVersion { get; set; } = string.Empty;
    [BindProperty]
    public bool IsAutoSwitchEnabled { get; set; }

    public UrlConnection CurrentUrlConnection { get; set; }
    public List<UrlConnection> UrlConnections { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public bool IsRemoveModalOpen { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool IsAddModalOpen { get; set; }

    #region
    [BindProperty]
    public string MoneroNodeUrl { get; set; } = string.Empty;
    [BindProperty]
    public string? Username { get; set; } = string.Empty;
    [BindProperty]
    public string? Password { get; set; } = string.Empty;
    #endregion

    [BindProperty(SupportsGet = true)]
    public string? NodeUrlToRemove { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Currency { get; set; }
    public Dictionary<string, string> Currencies { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string Language { get; set; }
    public Dictionary<string, string> Languages { get; set; } = [];

    public SettingsModel(
        ILogger<SettingsModel> logger,
        IXmrNodeService xmrNodeService,
        IVersionService versionService,
        IPriceService priceService,
        IOptions<RequestLocalizationOptions> localizationOptions)
    {
        _logger = logger;
        _xmrNodeService = xmrNodeService;
        _versionService = versionService;
        _priceService = priceService;
        _localizationOptions = localizationOptions;
    }

    public async Task Initialize()
    {
        HavenoVersion = await _versionService.GetVersionAsync();
        IsAutoSwitchEnabled = await _xmrNodeService.GetAutoSwitchAsync();

        UrlConnections.AddRange(await _xmrNodeService.GetConnectionsAsync());
        CurrentUrlConnection = await _xmrNodeService.GetConnectionAsync();

        Currencies = CurrencyHelper.FiatCurrenciesDictionary;
        if (string.IsNullOrEmpty(Currency))
            Currency = Request.Cookies["currency"]!;

        Response.Cookies.Append("currency", Currency);

        Languages = _localizationOptions.Value.SupportedUICultures!.ToDictionary(x => x.Name, y => y.NativeName);
        if (string.IsNullOrEmpty(Language))
            Language = Languages.First(x => x.Key == CultureInfo.CurrentUICulture.Name).Key;

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(new CultureInfo(Language))),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );
    }

    public async Task OnGetAsync()
    {
        await Initialize();
    }

    public async Task OnPostAddNodeAsync()
    {
        await _xmrNodeService.AddConnectionAsync(new UrlConnection
        {
            Username = Username ?? string.Empty,
            Password = Password ?? string.Empty,
            Url = MoneroNodeUrl
        });

        await Initialize();
    }

    public async Task OnPostRemoveNodeAsync()
    {
        if (string.IsNullOrEmpty(NodeUrlToRemove))
            return;

        await _xmrNodeService.RemoveConnectionAsync(NodeUrlToRemove);

        await Initialize();
    }

    public async Task OnPostConnectAsync(string url)
    {
        await _xmrNodeService.SetConnectionAsync(
            url,
            string.Empty,
            string.Empty,
            0);

        await Initialize();
    }

    //public async Task OnPostAutoSwitchToggleAsync(bool isToggled)
    //{
    //    await Initialize();
    //    IsAutoSwitchEnabled = isToggled;
    //}

    public async Task OnPostAutoSwitchToggleAsync()
    {
        await _xmrNodeService.SetAutoSwitchAsync(IsAutoSwitchEnabled);

        await Initialize();
    }
}
