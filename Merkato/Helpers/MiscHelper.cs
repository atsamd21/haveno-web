using Haveno.Api.Services;

namespace Merkato.Helpers;

// TODO rename
public static class MiscHelper
{
    public static async Task<int> GetTradedWithPeerCount(ITradeService tradeService, string peerAddress)
    {
        var trades = await tradeService.GetTradesAsync(Haveno.Shared.Enums.Category.Closed);
        return trades.Where(x => x.TradePeerNodeAddress == peerAddress).Count();
    }
}
