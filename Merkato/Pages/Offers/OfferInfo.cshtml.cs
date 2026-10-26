using Haveno.Api.Models;
using Haveno.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages.Offers;

public class OfferInfoModel : PageModel
{
    private readonly IOffersService _offersService;

    public OfferInfo OfferInfo { get; set; }
    [BindProperty(SupportsGet = true)]
    public string OfferId { get; set; }

    public OfferInfoModel(IOffersService offersService)
    {
        _offersService = offersService;
    }

    public async Task OnGetAsync()
    {
        OfferInfo = await _offersService.GetOfferAsync(OfferId);
    }
}
