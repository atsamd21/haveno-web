using Haveno.Api.Helpers;
using Haveno.Api.Models;
using Haveno.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages.Trades;

public class TradeModel : PageModel
{
    private readonly ITradeService _tradeService;
    private readonly IDisputeService _disputeService;

    public TradeInfo TradeInfo { get; set; }
    public TradeRoleAndStep TradeRoleAndStep { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IsMyAccountInfoModalOpen { get; set; }

    public TradeModel(
        ITradeService tradeService,
        IDisputeService disputeService)
    {
        _tradeService = tradeService;
        _disputeService = disputeService;
    }

    public async Task OnGetAsync(string tradeId)
    {
        TradeInfo = await _tradeService.GetTradeAsync(tradeId);
        TradeRoleAndStep = TradeHelper.GetTradeRoleAndStep(TradeInfo);

        HttpContext.Response.Headers.Append("Refresh", "30");
    }

    public async Task OnPostConfirmPaymentSentAsync(string tradeId)
    {
        await _tradeService.ConfirmPaymentSentAsync(tradeId);

        await Task.Delay(1_000);

        TradeInfo = await _tradeService.GetTradeAsync(tradeId);
        TradeRoleAndStep = TradeHelper.GetTradeRoleAndStep(TradeInfo);

        HttpContext.Response.Headers.Append("Refresh", "30");
    }

    public async Task OnPostConfirmPaymentReceivedAsync(string tradeId)
    {
        await _tradeService.ConfirmPaymentReceivedAsync(tradeId);

        await Task.Delay(1_000);

        TradeInfo = await _tradeService.GetTradeAsync(tradeId);
        TradeRoleAndStep = TradeHelper.GetTradeRoleAndStep(TradeInfo);

        HttpContext.Response.Headers.Append("Refresh", "30");
    }

    public async Task<IActionResult> OnPostCompleteTradeAsync(string tradeId)
    {
        await _tradeService.CompleteTradeAsync(tradeId);
        return LocalRedirect("/Trades");
    }

    public async Task<IActionResult> OnPostOpenDisputeAsync(string tradeId)
    {
        await _disputeService.OpenDisputeAsync(tradeId);
        return LocalRedirect($"/Trades/{tradeId}/Messages?isDispute=true#last-message");
    }

    public IActionResult OnPostViewDisputeAsync(string tradeId)
    {
        return LocalRedirect($"/Trades/{tradeId}/Messages?isDispute=true#last-message");
    }
}
