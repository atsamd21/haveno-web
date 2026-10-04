using Haveno.Api.Models;
using Haveno.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages;

public class OffersModel : PageModel
{
    private readonly IOffersService _offersService;
    private readonly IPaymentAccountService _paymentAccountService;

    public IEnumerable<OfferInfo> Offers { get; private set; } = [];
    public IEnumerable<TradeStatistic> TradeStatistics { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public OfferDirection OfferDirection { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? TextSearch { get; set; }
    [BindProperty(SupportsGet = true)]
    public string Currency { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)]
    public bool ShowPrivateOffers { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool ShowMatchingOffers { get; set; }

    public bool IsPaymentAccountModalOpen { get; set; }
    public List<PaymentAccount> PaymentAccounts { get; set; } = [];

    public bool IsOfferInfoModalOpen { get; set; }
    public OfferInfo? OfferInfo { get; set; }

    public OffersModel(
        IPaymentAccountService paymentAccountService,
        IOffersService offersService)
    {
        _offersService = offersService;
        _paymentAccountService = paymentAccountService;
    }

    public async Task Initialize()
    {
        var offerDirection = OfferDirection switch
        {
            OfferDirection.BUY => OfferDirection.SELL,
            OfferDirection.SELL => OfferDirection.BUY,
            _ => OfferDirection.ANY
        };

        Offers = await _offersService.GetOffersAsync(Currency, offerDirection);
        PaymentAccounts.AddRange(await _paymentAccountService.GetPaymentAccountsAsync());
    }

    public async Task OnGetAsync()
    {
        await Initialize();
        HttpContext.Response.Headers.Append("Refresh", "30");
    }

    public async Task OnPostNoPaymentAccountAsync()
    {
        await Initialize();
        IsPaymentAccountModalOpen = true;
    }

    public IActionResult OnPostRedirectToOfferPage(string offerId)
    {
        return LocalRedirect($"/Offer/{offerId}");
    }

    public IActionResult OnPostRedirectToAccountPage()
    {
        return LocalRedirect("/Account");
    }

    public async Task OnPostViewOfferInfoAsync(string offerId)
    {
        await Initialize();
        IsOfferInfoModalOpen = true;
        OfferInfo = await _offersService.GetOfferAsync(offerId);
    }
}
