using Haveno.Api.Models;
using Haveno.Api.Services;

namespace Merkato.Models;

public class TradeCard
{
    public TradeInfo TradeInfo { get; }
    public ITradeService TradeService { get; }

    public TradeCard(TradeInfo tradeInfo, ITradeService tradeService)
    {
        TradeInfo = tradeInfo;
        TradeService = tradeService;
    }
}
