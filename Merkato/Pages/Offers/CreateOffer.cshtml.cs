using Haveno.Api.Models;
using Haveno.Api.Models.Requests;
using Haveno.Api.Services;
using Merkato.Helpers;
using Merkato.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Merkato.Pages.Offers;

public class CreateOfferModel : PageModel
{
    private readonly ILogger<CreateOfferModel> _logger;
    private readonly IOffersService _offersService;
    private readonly IPaymentAccountService _paymentAccountService;
    private readonly IPriceService _priceService;
    private readonly IWalletService _walletService;

    #region Step 1
    [BindProperty(SupportsGet = true)]
    public OfferDirection OfferDirection { get; set; } = OfferDirection.BUY;
    #endregion

    #region Step 2
    [BindProperty(SupportsGet = true)]
    public int TabIndex { get; set; }

    public PaymentAccount? PaymentAccount { get; set; }
    public List<PaymentAccount> PaymentAccounts { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? PaymentAccountId { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? CurrencyCode { get; set; }

    public Dictionary<string, string> PaymentAccountsDictionary { get; set; } = [];
    #endregion

    #region Step 3
    [BindProperty(SupportsGet = true)]
    public decimal? MaxAmount { get; set; }
    [BindProperty(SupportsGet = true)]
    public decimal? MinAmount { get; set; }
    [BindProperty(SupportsGet = true)]
    public decimal? MarketPriceMarginPercent { get; set; }
    [BindProperty(SupportsGet = true)]
    public decimal? PriceForOneXMR { get; set; }
    [BindProperty(SupportsGet = true)]
    public string ExtraInfo { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)]
    public decimal? TriggerPrice { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool UseMarketBasedPrice { get; set; } = true;
    [BindProperty(SupportsGet = true)]
    public bool ReserveExactAmount { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool IsNoDepositOffer { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool IsPrivateOffer { get; set; }
    [BindProperty(SupportsGet = true)]
    public decimal? PriceForTotalAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SecurityDepositPercent { get; set; } = "15";
    public Dictionary<string, string> SecurityDepositPercentDict { get; set; } = [];

    //
    public ulong TradeFeePiconero { get; set; }
    public ulong SecurityDepositAmountPiconero { get; set; }
    public ulong TotalRequiredFundsPiconero { get; set; }
    public bool UseMinimumSecurityDeposit { get; set; }
    public decimal TradeFeeInCounterCurrency { get; set; }
    // Whats the max that the user can sell/buy when fees and deposits are included
    public ulong MaxSpendableFunds { get; set; }
    public ulong AvailableBalance { get; set; }

    #endregion

    // All
    [BindProperty(SupportsGet = true)]
    public int Step { get; set; }

    // Fetch price once on initial
    [BindProperty(SupportsGet = true)]
    public decimal? Price { get; set; }

    public CreateOfferModel(
        ILogger<CreateOfferModel> logger,
        IOffersService offersService,
        IPaymentAccountService paymentAccountService,
        IWalletService walletService,
        IPriceService priceService)
    {
        _logger = logger;
        _offersService = offersService;
        _paymentAccountService = paymentAccountService;
        _walletService = walletService;
        _priceService = priceService;
    }

    private async Task Initialize()
    {
        if (IsNoDepositOffer)
            IsPrivateOffer = true;

        for (int i = 15; i <= 50; i++)
        {
            SecurityDepositPercentDict.Add(i.ToString(), $"{i}%");
        }

        var paymentAccounts = await _paymentAccountService.GetPaymentAccountsAsync();

        PaymentAccounts = FilterHelper.FilterCryptoOrTraditionalPaymentAccounts(
            paymentAccounts,
            TabIndex == 0 ? PaymentAccountFilter.Traditional : PaymentAccountFilter.Crypto).ToList();

        if (PaymentAccounts.Count != 0)
        {
            PaymentAccount = PaymentAccounts.FirstOrDefault(x => x.Id == PaymentAccountId);
            if (PaymentAccount is null)
            {
                PaymentAccount = PaymentAccounts.First();
                PaymentAccountId = PaymentAccount.Id;
                CurrencyCode = PaymentAccount.TradeCurrencies.First().Code;
            }
            else
            {
                PaymentAccount = PaymentAccounts.First(x => x.Id == PaymentAccountId);
                // Other fields are supplied by hidden inputs
            }
        }

        // Only needed for step 1
        PaymentAccountsDictionary = PaymentAccounts.ToDictionary(x => x.Id, x => x.AccountName);

        try
        {
            if (Price is null)
            {
                // This is very slow when it throws
                Price = await _priceService.GetMarketPriceAsync(CurrencyCode);
            }
        }
        catch
        {
            UseMarketBasedPrice = false;
        }

        if (Step == 2)
        {
            await CalculateMaxSpendableFundsAsync();

            if (MaxAmount is not null)
                await CalculateRequiredFundsAsync(MaxAmount.Value.ToPiconero());
        }
    }

    private ulong CalculateTradeFeeAmountPiconero(ulong piconeroAmount)
    {
        var isFiat = TabIndex == 0;

        decimal feePercent;
        if (isFiat)
            feePercent = AppConstants.MakerFeePctTraditional;
        else
            feePercent = AppConstants.MakerFeePctCrypto;

        if (OfferDirection == OfferDirection.SELL && IsNoDepositOffer)
        {
            if (isFiat)
                feePercent += AppConstants.TakerFeePctTraditional;
            else
                feePercent += AppConstants.TakerFeePctCrypto;
        }

        return (ulong)(piconeroAmount * feePercent);
    }

    private ulong CalculateDepositAmountPiconero(ulong piconeroAmount)
    {
        var securityDepositPercent = decimal.Parse(SecurityDepositPercent, CultureInfo.InvariantCulture) / 100m;
        ulong securityDepositAmount = (ulong)(piconeroAmount * securityDepositPercent);

        if (securityDepositAmount < AppConstants.MinSecurityDepositAmountPiconero)
        {
            return AppConstants.MinSecurityDepositAmountPiconero;
        }

        return securityDepositAmount;
    }

    private async Task CalculateMaxSpendableFundsAsync()
    {
        var balances = await _walletService.GetBalancesAsync();
        decimal availableFundsPiconero = balances.AvailableBalance;
        AvailableBalance = balances.AvailableBalance;

        if (AvailableBalance == 0)
        {
            MaxSpendableFunds = 0;
            return;
        }

        var isFiat = TabIndex == 0;

        decimal feePercent;
        if (isFiat)
            feePercent = AppConstants.MakerFeePctTraditional;
        else
            feePercent = AppConstants.MakerFeePctCrypto;

        if (OfferDirection == OfferDirection.SELL && IsNoDepositOffer)
        {
            if (isFiat)
                feePercent += AppConstants.TakerFeePctTraditional;
            else
                feePercent += AppConstants.TakerFeePctCrypto;
        }

        var securityDepositPercent = decimal.Parse(SecurityDepositPercent, CultureInfo.InvariantCulture) / 100m;

        if (OfferDirection == OfferDirection.BUY)
        {
            decimal min = (availableFundsPiconero - AppConstants.MinSecurityDepositAmountPiconero) / feePercent;

            decimal normal = availableFundsPiconero / (feePercent + securityDepositPercent);

            var max = Math.Min(normal, min);
            if (max < 0m)
                MaxSpendableFunds = 0;
            else
                MaxSpendableFunds = (ulong)max;
        }
        else
        {
            decimal min = (availableFundsPiconero - AppConstants.MinSecurityDepositAmountPiconero) / (1m + feePercent);

            decimal normal = availableFundsPiconero / (1m + feePercent + securityDepositPercent);

            var max = Math.Min(normal, min);
            if (max < 0m)
                MaxSpendableFunds = 0;
            else
                MaxSpendableFunds = (ulong)max;

            if (IsNoDepositOffer && (1.5m.ToPiconero() < MaxSpendableFunds))
            {
                MaxSpendableFunds = 1.5m.ToPiconero();
            }
        }
    }

    private async Task CalculateRequiredFundsAsync(ulong piconeroAmount)
    {
        var priceFetchTask = _priceService.GetMarketPriceAsync(CurrencyCode!);

        TradeFeePiconero = CalculateTradeFeeAmountPiconero(piconeroAmount);
        SecurityDepositAmountPiconero = CalculateDepositAmountPiconero(piconeroAmount);

        if (SecurityDepositAmountPiconero == AppConstants.MinSecurityDepositAmountPiconero)
        {
            UseMinimumSecurityDeposit = true;
        }

        if (OfferDirection == OfferDirection.BUY)
        {
            TotalRequiredFundsPiconero = SecurityDepositAmountPiconero + TradeFeePiconero;
        }
        else
        {
            TotalRequiredFundsPiconero = piconeroAmount + SecurityDepositAmountPiconero + TradeFeePiconero;
        }

        try
        {
            var isFiat = TabIndex == 0;
            TradeFeeInCounterCurrency = Math.Round(TradeFeePiconero.ToMonero() * await priceFetchTask, isFiat ? 2 : 8, MidpointRounding.AwayFromZero);
        }
        catch
        {
            // Could not fetch price, might have no price data
        }
    }

    public async Task OnGetAsync()
    {
        await Initialize();
    }

    public async Task OnPostLastStepAsync()
    {
        Step = 3;

        await OnPostCalculateAsync();

        await CalculateMaxSpendableFundsAsync();

        if (MaxAmount is not null)
            await CalculateRequiredFundsAsync(MaxAmount.Value.ToPiconero());
    }

    public async Task OnPostPreviousStepAsync()
    {
        Step = 2;
        await Initialize();

        // redirect with query params?
        // if you naviagate back the toggles reset the form
    }

    public async Task<IActionResult> OnPostCalculateAsync()
    {
        await Initialize();

        // Checked on frontend
        if (MaxAmount is null || MaxAmount.Value < 0.05m)
            throw new Exception();

        if (MinAmount is null || MinAmount.Value < 0.05m)
        {
            MinAmount = MaxAmount;
        }
        else
        {
            if (MaxAmount < MinAmount)
            {
                MinAmount = MaxAmount;
            }
        }

        if (UseMarketBasedPrice)
        {
            // Already checked
            if (MarketPriceMarginPercent is null)
                throw new Exception();

            if (OfferDirection == OfferDirection.BUY)
            {
                // Below market
                PriceForOneXMR = (1m - (MarketPriceMarginPercent.Value / 100m)) * Price;
            }
            else
            {
                // Above
                PriceForOneXMR = (1m + (MarketPriceMarginPercent.Value / 100m)) * Price;
            }
        }
        else
        {
            // User sets this
            if (PriceForOneXMR is null)
                throw new Exception();

            if (Price is not null)
            {
                MarketPriceMarginPercent = Math.Round((1m - (PriceForOneXMR.Value / Price.Value)) * 100m, 2, MidpointRounding.AwayFromZero);
            }
        }

        if (TabIndex == 0)
        {
            // If traditional
            PriceForOneXMR = Math.Round(PriceForOneXMR.Value, 4, MidpointRounding.AwayFromZero);
            PriceForTotalAmount = Math.Round(PriceForOneXMR.Value * MaxAmount.Value, MidpointRounding.AwayFromZero);

            var amount = Math.Round(PriceForTotalAmount.Value / PriceForOneXMR.Value, 4, MidpointRounding.AwayFromZero);
            if (MaxAmount == MinAmount)
            {
                MinAmount = amount;
            }
            else
            {
                var priceForTotalAmountMinimum = Math.Round(PriceForOneXMR.Value * MinAmount.Value, MidpointRounding.AwayFromZero);
                MinAmount = Math.Round(priceForTotalAmountMinimum / PriceForOneXMR.Value, 4, MidpointRounding.AwayFromZero);
            }

            MaxAmount = amount;
        }
        else
        {
            // Crypto
            PriceForOneXMR = Math.Round(PriceForOneXMR.Value, 8, MidpointRounding.AwayFromZero);
            PriceForTotalAmount = Math.Round(PriceForOneXMR.Value * MaxAmount.Value, 8, MidpointRounding.AwayFromZero);
        }

        // Needed to preserve url query params for toggles
        return RedirectToPage(new
        {
            OfferDirection,
            CurrencyCode,
            Step,
            PaymentAccountId,
            MaxAmount,
            MinAmount,
            MarketPriceMarginPercent,
            PriceForOneXMR,
            ExtraInfo,
            SecurityDepositPercent,
            TriggerPrice,
            UseMarketBasedPrice,
            IsNoDepositOffer,
            ReserveExactAmount,
            IsPrivateOffer,
            PriceForTotalAmount,
            TabIndex,
            Price
        });
    }

    public async Task<IActionResult> OnPostCreateOfferAsync()
    {
        // Property?
        var securityDepositPercent = decimal.Parse(SecurityDepositPercent, CultureInfo.InvariantCulture);

        PostOfferRequest postOfferRequest = new()
        {
            PaymentAccountId = PaymentAccountId!,
            CurrencyCode = CurrencyCode!,
            Amount = MaxAmount!.Value.ToPiconero(),
            MinAmount = MinAmount!.Value.ToPiconero(),
            Direction = OfferDirection,
            SecurityDepositPercent = (double)(securityDepositPercent / 100m),
            ReserveExactAmount = ReserveExactAmount,
            ExtraInfo = ExtraInfo,
            TriggerPrice = TriggerPrice ?? 0m,
            UseMarketBasedPrice = UseMarketBasedPrice,
            // If is no deposit its also a private offer
            IsNoDepositOffer = IsNoDepositOffer,
            IsPrivateOffer = IsPrivateOffer
        };

        if (UseMarketBasedPrice)
        {
            postOfferRequest.MarketPriceMarginPercent = (double)(MarketPriceMarginPercent / 100m);
        }
        else
        {
            postOfferRequest.Price = PriceForOneXMR.Value;
        }

        await _offersService.PostOfferAsync(postOfferRequest);

        return LocalRedirect("/Offers/MyOffers");
    }
}
