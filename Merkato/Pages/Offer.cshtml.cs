using Haveno.Api.Models;
using Haveno.Api.Models.Requests;
using Haveno.Api.Services;
using Merkato.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages;

public class OfferModel : PageModel
{
    private readonly IOffersService _offersService;
    private readonly IPaymentAccountService _paymentAccountService;
    private readonly ITradeService _tradeService;

    [BindProperty]
    public decimal XmrAmount { get; set; }
    [BindProperty]
    public string PaymentAccountId { get; set; }

    public OfferInfo OfferInfo { get; set; }
    public List<PaymentAccount> PaymentAccounts { get; set; } = [];

    public bool IsModalOpen { get; set; }

    public OfferModel(
        IOffersService offersService,
        ITradeService tradeService,
        IPaymentAccountService paymentAccountService)
    {
        _offersService = offersService;
        _tradeService = tradeService;
        _paymentAccountService = paymentAccountService;
    }

    //[MemberNotNull(nameof(OfferInfo))]
    public async Task Initialize(string offerId)
    {
        OfferInfo = await _offersService.GetOfferAsync(offerId);

        foreach (var paymentAccount in await _paymentAccountService.GetPaymentAccountsAsync())
        {
            if (OfferInfo.PaymentMethodId == PaymentMethodId.BLOCK_CHAINS || 
                OfferInfo.PaymentMethodId == PaymentMethodId.BLOCK_CHAINS_INSTANT)
            {
                if ((paymentAccount.PaymentMethod.Id == PaymentMethodId.BLOCK_CHAINS ||
                    paymentAccount.PaymentMethod.Id == PaymentMethodId.BLOCK_CHAINS_INSTANT) &&
                    paymentAccount.TradeCurrencies.First().Code == OfferInfo.CounterCurrencyCode)
                {
                    PaymentAccounts.Add(paymentAccount);
                }
            }
            else if (paymentAccount.PaymentMethod.Id == OfferInfo.PaymentMethodId)
            {
                PaymentAccounts.Add(paymentAccount);
            }
        }
    }

    //public async IAsyncEnumerable<PaymentAccount> GetFilteredPaymentAccounts()
    //{
    //    foreach (var paymentAccount in await _paymentAccountService.GetPaymentAccountsAsync())
    //    {
    //        if (paymentAccount.PaymentMethod.Id == OfferInfo.PaymentMethodId)
    //        {
    //            yield return paymentAccount;
    //        }
    //    }
    //}

    public async Task OnGetAsync(string offerId)
    {
        await Initialize(offerId);

        PaymentAccountId = PaymentAccounts.FirstOrDefault()?.Id ?? string.Empty;
        XmrAmount = OfferInfo.Amount.ToMonero();
    }

    public async Task OnPostReviewTakeOfferAsync(string offerId)
    {
        await Initialize(offerId);
        IsModalOpen = true;
    }

    public async Task<IActionResult> OnPostTakeOfferAsync(string offerId)
    {
        await Initialize(offerId);

        var paymentAccount = PaymentAccounts
            .FirstOrDefault(x => x.Id == PaymentAccountId);

        var response = await _tradeService.TakeOfferAsync(new TakeOfferRequest
        {
            OfferId = offerId,
            PaymentAccountId = PaymentAccountId,
            Amount = XmrAmount.ToPiconero(),
            //Challenge
        });

        return LocalRedirect($"/Trade/{response.Trade.TradeId}");
    }
}
