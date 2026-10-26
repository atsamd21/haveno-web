using Haveno.Api.Models;
using Haveno.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages.Trades;

public class TradesModel : PageModel
{
    private readonly ITradeService _tradeService;
    private readonly IDisputeService _disputeService;

    public List<TradeInfo> OpenTrades { get; set; } = [];
    public List<TradeInfo> ClosedTrades { get; set; } = [];
    public List<TradeInfo> FailedTrades { get; set; } = [];
    public List<Dispute> Disputes { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int TabIndex { get; set; }

    public TradesModel(
        IDisputeService disputeService,
        ITradeService tradeService)
    {
        _tradeService = tradeService;
        _disputeService = disputeService;
    }

    public async Task OnGetAsync()
    {
        var openTradesTask = _tradeService.GetTradesAsync(Haveno.Shared.Enums.Category.Open);
        var closedTradesTask = _tradeService.GetTradesAsync(Haveno.Shared.Enums.Category.Closed);
        var failedTradesTask = _tradeService.GetTradesAsync(Haveno.Shared.Enums.Category.Failed);
        var disputesTask = _disputeService.GetDisputesAsync();

        OpenTrades.AddRange(await openTradesTask);
        ClosedTrades.AddRange(await closedTradesTask);
        FailedTrades.AddRange(await failedTradesTask);
        Disputes.AddRange(await disputesTask);
    }
}
