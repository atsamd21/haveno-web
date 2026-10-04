using Haveno.Api.Models;
using Haveno.Api.Models.Requests;
using Haveno.Api.Services;
using Merkato.Helpers;
using Merkato.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Merkato.Pages;

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
    public Tabs Tabs { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool CloseModal { get; set; }
    public Modal? NoPaymentAccountsModal { get; set; }

    public Dropdown PaymentAccountsDropdown { get; set; }
    public PaymentAccount? PaymentAccount { get; set; }
    public List<PaymentAccount> PaymentAccounts { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? PaymentAccountId { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? CurrencyCode { get; set; }
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
    //[BindProperty(SupportsGet = true)]
    //public decimal SecurityDepositPercent { get; set; } = 15;
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

    // 
    [BindProperty(SupportsGet = true)]
    public bool UseExactReceiveAmount { get; set; }

    // 
    [BindProperty(SupportsGet = true)]
    public string SecurityDepositPercent { get; set; } = "15";
    public Dictionary<string, string> SecurityDepositPercentDict { get; set; } = [];

    //
    public ulong TradeFee { get; set; }
    public ulong RequiredFunds { get; set; }
    public ulong SecurityDepositAmount { get; set; }
    public bool UseMinimumSecurityDeposit { get; set; }
    public decimal TradeFeeInCounterCurrency { get; set; }

    #endregion

    // All
    [BindProperty(SupportsGet = true)]
    public int Step { get; set; }

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
        // ?
        if (IsNoDepositOffer)
            IsPrivateOffer = true;

        for (int i = 15; i <= 50; i++)
        {
            SecurityDepositPercentDict.Add(i.ToString(), $"{i}%");
        }

        Tabs = new(["Traditional", "Crypto"], TabIndex);

        var paymentAccounts = await _paymentAccountService.GetPaymentAccountsAsync();

        PaymentAccounts = FilterHelper.FilterCryptoOrTraditionalPaymentAccounts(
            paymentAccounts,
            TabIndex == 0 ? PaymentAccountFilter.Traditional : PaymentAccountFilter.Crypto).ToList();

        if (PaymentAccounts.Count == 0)
        {
            if (!CloseModal && Step == 1)
            {
                NoPaymentAccountsModal = new Modal("No payment accounts", "RedirectToAccount")
                {
                    SubmitButtonText = "Set up a new trading account",
                    Information = ["You don't have a payment account set up for the selected currency."],
                    CloseParameters = new Dictionary<string, string> 
                    { 
                        { "CloseModal", "true" } 
                    }
                };
            }
        }
        else
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
            }
        }

        // ?
        if (Step == 2 || Step == 3)
        {
            await CalculateRequiredFundsAsync();
        }

        // Only needed for step 1
        PaymentAccountsDropdown = new Dropdown(
            PaymentAccounts.ToDictionary(x => x.Id, x => x.AccountName),
            PaymentAccountId);
    }

    public async Task CalculateRequiredFundsAsync()
    {
        if (MaxAmount is null)
            return;

        var isFiat = TabIndex == 0;
        var piconeroAmount = MaxAmount.Value.ToPiconero();
        var securityDepositPercent = decimal.Parse(SecurityDepositPercent, CultureInfo.InvariantCulture);

        if (OfferDirection == OfferDirection.BUY)
        {
            if (isFiat)
                TradeFee = (ulong)(piconeroAmount * AppConstants.MakerFeePctTraditional);
            else
                TradeFee = (ulong)(piconeroAmount * AppConstants.MakerFeePctCrypto);

            SecurityDepositAmount = (ulong)(piconeroAmount * (securityDepositPercent / 100m));

            if (SecurityDepositAmount < AppConstants.MinSecurityDepositAmountPiconero)
            {
                SecurityDepositAmount = AppConstants.MinSecurityDepositAmountPiconero;
                UseMinimumSecurityDeposit = true;
            }

            RequiredFunds = SecurityDepositAmount + TradeFee;
        }
        else
        {
            double tradeFeePct;

            if (isFiat)
                tradeFeePct = IsNoDepositOffer ? (AppConstants.MakerFeePctTraditional + AppConstants.TakerFeePctTraditional) : AppConstants.MakerFeePctTraditional;
            else
                tradeFeePct = IsNoDepositOffer ? (AppConstants.MakerFeePctCrypto + AppConstants.TakerFeePctCrypto) : AppConstants.MakerFeePctCrypto;

            TradeFee = (ulong)(piconeroAmount * tradeFeePct);
            SecurityDepositAmount = (ulong)(piconeroAmount * (securityDepositPercent / 100m));

            if (SecurityDepositAmount < AppConstants.MinSecurityDepositAmountPiconero)
            {
                SecurityDepositAmount = AppConstants.MinSecurityDepositAmountPiconero;
                UseMinimumSecurityDeposit = true;
            }

            RequiredFunds = piconeroAmount + SecurityDepositAmount + TradeFee;
        }

        var price = await _priceService.GetMarketPriceAsync(CurrencyCode);
        TradeFeeInCounterCurrency = Math.Round(TradeFee.ToMonero() * price, isFiat ? 2 : 8);
    }

    public IActionResult OnPostRedirectToAccount()
    {
        return LocalRedirect("/Account");
    }

    public async Task OnGetAsync()
    {
        await Initialize();
    }

    // ?
    public async Task OnPostLastStepAsync()
    {
        Step = 3;
        //await Initialize();

        // temp
        await OnPostCalculateAsync();
    }

    public async Task OnPostPreviousStepAsync()
    {
        Step = 2;
        await Initialize();
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

        var price = await _priceService.GetMarketPriceAsync(CurrencyCode);

        if (UseMarketBasedPrice)
        {
            // Already checked
            if (MarketPriceMarginPercent is null)
                throw new Exception();

            if (OfferDirection == OfferDirection.BUY)
            {
                // Below market
                PriceForOneXMR = (1m - (MarketPriceMarginPercent.Value / 100m)) * price;
            }
            else
            {
                // Above
                PriceForOneXMR = (1m + (MarketPriceMarginPercent.Value / 100m)) * price;
            }
        }
        else
        {
            // User sets this
            if (PriceForOneXMR is null)
                throw new Exception();

            MarketPriceMarginPercent = Math.Round((1m - (PriceForOneXMR.Value / price)) * 100m, 2);
        }

        // TODO Check this, should PriceForOneXMR be rounded first?
        if (TabIndex == 0)
        {
            // If traditional
            PriceForTotalAmount = Math.Round(PriceForOneXMR.Value * MaxAmount.Value);
            PriceForOneXMR = Math.Round(PriceForOneXMR.Value, 4);
        }
        else
        {
            // Crypto
            PriceForTotalAmount = Math.Round(PriceForOneXMR.Value * MaxAmount.Value, 8);
            PriceForOneXMR = Math.Round(PriceForOneXMR.Value, 8);
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
            PriceForTotalAmount
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
            TriggerPrice = TriggerPrice ?? 0,
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

        return LocalRedirect("/MyOffers");
    }
}
