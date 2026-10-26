using Haveno.Api.Models;
using Haveno.Api.Models.Requests;
using Haveno.Api.Services;
using Merkato.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages.Offers;

public class EditOfferModel : PageModel
{
    private readonly IOffersService _offersService;
    private readonly IPriceService _priceService;
    private readonly IWalletService _walletService;
    private readonly IPaymentAccountService _paymentAccountService;

    public OfferInfo OfferInfo { get; set; }
    public PaymentAccount? PaymentAccount { get; set; }
    public decimal? PriceForTotalAmount { get; set; }

    [BindProperty]
    public string CurrencyCode { get; set; }
    [BindProperty]
    public string PaymentAccountId { get; set; }
    [BindProperty]
    public decimal? PriceForOneXMR { get; set; }


    [BindProperty(SupportsGet = true)]
    public bool UseMarketBasedPrice { get; set; }
    [BindProperty]
    public decimal? MarketPriceMarginPercent { get; set; }
    [BindProperty]
    public decimal? TriggerPrice { get; set; }
    [BindProperty]
    public string? ExtraInfo { get; set; }
    [BindProperty(SupportsGet = true)]
    public int Step { get; set; }

    //
    [BindProperty(SupportsGet = true)]
    public bool HasLoaded { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? Price { get; set; }

    public EditOfferModel(
        IOffersService offersService, 
        IPriceService priceService,
        IWalletService walletService,
        IPaymentAccountService paymentAccountService)
    {
        _offersService = offersService;
        _priceService = priceService;
        _walletService = walletService;
        _paymentAccountService = paymentAccountService;
    }

    public async Task Initialize(string offerId)
    {
        OfferInfo = await _offersService.GetMyOfferAsync(offerId);
        CurrencyCode = OfferInfo.CounterCurrencyCode;
        PaymentAccountId = OfferInfo.PaymentAccountId;

        if (!HasLoaded)
        {
            UseMarketBasedPrice = OfferInfo.UseMarketBasedPrice;
            TriggerPrice = OfferInfo.TriggerPrice == 0 ? null : OfferInfo.TriggerPrice;
            ExtraInfo = OfferInfo.ExtraInfo;

            if (OfferInfo.UseMarketBasedPrice)
            {
                Price = await _priceService.GetMarketPriceAsync(CurrencyCode);
                MarketPriceMarginPercent = (decimal?)(OfferInfo.MarketPriceMarginPercent * 100);
            }
            else
                PriceForOneXMR = OfferInfo.Price;

            // Might need to be put in url for toggle
            HasLoaded = true;
        }
    }

    private async Task Calculate()
    {
        //var price = await _priceService.GetMarketPriceAsync(OfferInfo.CounterCurrencyCode);

        if (UseMarketBasedPrice)
        {
            if (MarketPriceMarginPercent is null)
                throw new Exception();

            if (OfferInfo.Direction == OfferDirection.BUY)
            {
                PriceForOneXMR = (1m - (MarketPriceMarginPercent.Value / 100m)) * Price;
            }
            else
            {
                PriceForOneXMR = (1m + (MarketPriceMarginPercent.Value / 100m)) * Price;
            }
        }
        else
        {
            if (PriceForOneXMR is null)
                throw new Exception();

            if (Price is not null)
            {
                MarketPriceMarginPercent = Math.Round((1m - (PriceForOneXMR.Value / Price.Value)) * 100m, 2, MidpointRounding.AwayFromZero);
            }
        }

        if (OfferInfo.IsFiat())
        {
            PriceForOneXMR = Math.Round(PriceForOneXMR.Value, 4, MidpointRounding.AwayFromZero);
            PriceForTotalAmount = Math.Round(PriceForOneXMR.Value * OfferInfo.Amount.ToMonero(), MidpointRounding.AwayFromZero);
        }
        else
        {
            PriceForOneXMR = Math.Round(PriceForOneXMR.Value, 8, MidpointRounding.AwayFromZero);
            PriceForTotalAmount = Math.Round(PriceForOneXMR.Value * OfferInfo.Amount.ToMonero(), 8, MidpointRounding.AwayFromZero);
        }
    }

    public async Task OnGetAsync(string offerId)
    {
        await Initialize(offerId);
        await Calculate();
    }

    public async Task OnPostCalculateAsync(string offerId)
    {
        await Initialize(offerId);
        await Calculate();
    }

    public async Task OnPostNextStepAsync(string offerId)
    {
        await Initialize(offerId);
        Step = 1;

        await Calculate();

        var paymentAccounts = await _paymentAccountService.GetPaymentAccountsAsync();
        PaymentAccount = paymentAccounts.FirstOrDefault(x => x.Id == PaymentAccountId);
    }

    public async Task OnPostPreviousStepAsync(string offerId)
    {
        await Initialize(offerId);
        Step = 0;

        await Calculate();
    }

    public async Task<IActionResult> OnPostEditOfferAsync(string offerId)
    {
        await Initialize(offerId);

        var request = new EditOfferRequest
        {
            OfferId = offerId,
            UseMarketBasedPrice = UseMarketBasedPrice,

            CurrencyCode = OfferInfo.CounterCurrencyCode,
            PaymentAccountId = OfferInfo.PaymentAccountId,

            TriggerPrice = TriggerPrice ?? 0m,
        };

        if (UseMarketBasedPrice)
        {
            request.MarketPriceMarginPercent = (double)(MarketPriceMarginPercent!.Value / 100m);
        }
        else
        {
            request.Price = PriceForOneXMR!.Value;
        }

        // Hacky but also the backend appends the payment accounts extra info each time
        if (!string.IsNullOrEmpty(ExtraInfo) && ExtraInfo != OfferInfo.ExtraInfo.Replace("\n", ""))
        {
            request.ExtraInfo = ExtraInfo;
        }

        await _offersService.EditOfferAsync(request);

        return LocalRedirect("/Offers/MyOffers");
    }
}
