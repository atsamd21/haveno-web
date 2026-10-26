using Haveno.Api.Models;
using Haveno.Api.Models.Requests;
using Haveno.Api.Services;
using Merkato.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages.Offers;

public class OfferModel : PageModel
{
    private readonly IOffersService _offersService;
    private readonly IPaymentAccountService _paymentAccountService;
    private readonly ITradeService _tradeService;

    [BindProperty(SupportsGet = true)]
    public decimal XmrAmount { get; set; }
    [BindProperty(SupportsGet = true)]
    public string PaymentAccountId { get; set; }
    [BindProperty(SupportsGet = true)]
    public decimal CounterCurrencyAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool UseCounterCurrency { get; set; }

    [BindProperty]
    public string? Passphrase { get; set; }

    public OfferInfo OfferInfo { get; set; }
    public List<PaymentAccount> PaymentAccounts { get; set; } = [];
    public PaymentAccount? PaymentAccount { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IsTakeOfferModalOpen { get; set; }

    // Initialized?
    [BindProperty(SupportsGet = true)]
    public bool HasLoaded { get; set; }

    public OfferModel(
        IOffersService offersService,
        ITradeService tradeService,
        IPaymentAccountService paymentAccountService)
    {
        _offersService = offersService;
        _tradeService = tradeService;
        _paymentAccountService = paymentAccountService;
    }

    public async Task Initialize(string offerId)
    {
        OfferInfo = await _offersService.GetOfferAsync(offerId);

        foreach (var paymentAccount in await _paymentAccountService.GetPaymentAccountsAsync())
        {
            if (OfferInfo.IsCrypto())
            {
                if ((paymentAccount.PaymentMethod.Id == PaymentMethodId.BLOCK_CHAINS ||
                    paymentAccount.PaymentMethod.Id == PaymentMethodId.BLOCK_CHAINS_INSTANT) &&
                    paymentAccount.TradeCurrencies.First().Code == OfferInfo.CounterCurrencyCode)
                {
                    PaymentAccounts.Add(paymentAccount);
                }
            }
            else if (paymentAccount.PaymentMethod.Id == OfferInfo.PaymentMethodId && 
                paymentAccount.TradeCurrencies.Select(x => x.Code).Contains(OfferInfo.CounterCurrencyCode))
            {
                PaymentAccounts.Add(paymentAccount);
            }
        }

        if (string.IsNullOrEmpty(PaymentAccountId))
        {
            PaymentAccountId = PaymentAccounts.FirstOrDefault()?.Id ?? string.Empty;
        }

        PaymentAccount = PaymentAccounts.FirstOrDefault(x => x.Id == PaymentAccountId);
    }

    public async Task OnGetAsync(string offerId)
    {
        await Initialize(offerId);

        if (!HasLoaded)
        {
            if (UseCounterCurrency)
            {
                CounterCurrencyAmount = OfferInfo.CounterCurrencyVolume;
            }
            else
            {
                XmrAmount = OfferInfo.Amount.ToMonero();
            }

            HasLoaded = true;
        }

        if (IsTakeOfferModalOpen)
        {
            HttpContext.Response.Headers.Append("Refresh", "60");
        }

        CalculatePrice();
    }

    public async Task<IActionResult> OnPostReviewTakeOfferAsync(string offerId)
    {
        await Initialize(offerId);
        IsTakeOfferModalOpen = true;

        // So that the page can be refreshed and keep state
        return RedirectToPage(new {
            IsTakeOfferModalOpen,
            HasLoaded,
            XmrAmount,
            PaymentAccountId,
            CounterCurrencyAmount,
            UseCounterCurrency
        });
    }

    public async Task OnPostCalculatePriceAsync(string offerId)
    {
        await Initialize(offerId);
        CalculatePrice();
    }

    private void CalculatePrice()
    {
        if (UseCounterCurrency)
        {
            XmrAmount = Math.Round(CounterCurrencyAmount / OfferInfo.Price, 4, MidpointRounding.AwayFromZero);

            XmrAmount = Math.Clamp(
                XmrAmount,
                OfferInfo.MinAmount.ToMonero(),
                OfferInfo.Amount.ToMonero());

            CounterCurrencyAmount = Math.Round(OfferInfo.Price * XmrAmount, OfferInfo.IsFiat() ? 0 : 8, MidpointRounding.AwayFromZero);
        }
        else
        {
            CounterCurrencyAmount = Math.Round(
                OfferInfo.Price * XmrAmount, 
                OfferInfo.IsFiat() ? 0 : 8, 
                MidpointRounding.AwayFromZero);

            CounterCurrencyAmount = Math.Clamp(
                CounterCurrencyAmount, 
                OfferInfo.CounterCurrencyMinVolume, 
                OfferInfo.CounterCurrencyVolume);

            XmrAmount = Math.Clamp(
                Math.Round(CounterCurrencyAmount / OfferInfo.Price, 4, MidpointRounding.AwayFromZero),
                OfferInfo.MinAmount.ToMonero(),
                OfferInfo.Amount.ToMonero());
        }
    }

    public async Task<IActionResult> OnPostTakeOfferAsync(string offerId)
    {
        await Initialize(offerId);

        var request = new TakeOfferRequest
        {
            OfferId = offerId,
            PaymentAccountId = PaymentAccountId,
            Amount = XmrAmount.ToPiconero()
        };

        if (!string.IsNullOrEmpty(Passphrase))
            request.Challenge = Passphrase.Trim();

        var response = await _tradeService.TakeOfferAsync(request);

        return LocalRedirect($"/Trades/{response.Trade.TradeId}");
    }
}
