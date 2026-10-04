using Haveno.Api.Models;
using Haveno.Api.Models.Requests;
using Haveno.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages;

public class MyOffersModel : PageModel
{
    private readonly ILogger<MyOffersModel> _logger;
    private readonly IOffersService _offersService;

    public List<OfferInfo> Offers { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int TabIndex { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool IsOfferInfoModalOpen { get; set; }

    public OfferInfo? OfferInfo { get; set; }

    public MyOffersModel(
        ILogger<MyOffersModel> logger,
        IOffersService offersService)
    {
        _logger = logger;
        _offersService = offersService;
    }

    public async Task Initialize()
    {
        Offers.AddRange(await _offersService.GetMyOffersAsync("", OfferDirection.ANY));
    }

    public async Task OnGetAsync()
    {
        await Initialize();
        //HttpContext.Response.Headers.Append("Refresh", "60");
    }

    public async Task OnPostViewOfferInfoAsync(string offerId)
    {
        IsOfferInfoModalOpen = true;
        OfferInfo = await _offersService.GetMyOfferAsync(offerId);
        await Initialize();
    }

    public async Task OnPostRemoveOfferAsync(string offerId)
    {
        await _offersService.CancelOfferAsync(offerId);
        await Initialize();
    }
}
