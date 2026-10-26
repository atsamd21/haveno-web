using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages.Offers;

public class TakeOfferModel : PageModel
{

    public TakeOfferModel()
    {

    }

    public async Task<IActionResult> OnGetAsync(string offerId)
    {
        //var request = new TakeOfferRequest
        //{
        //    OfferId = offerId,
        //    PaymentAccountId = PaymentAccountId,
        //    Amount = XmrAmount.ToPiconero()
        //};

        //if (!string.IsNullOrEmpty(Passphrase))
        //    request.Challenge = Passphrase.Trim();

        //var response = await _tradeService.TakeOfferAsync(request);

        //return LocalRedirect($"/Trades/{response.Trade.TradeId}");

        return Page();
    }
}
