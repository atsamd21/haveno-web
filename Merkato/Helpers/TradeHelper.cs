using Haveno.Api.Models;

namespace Merkato.Helpers;

public static class TradeHelper
{
    public static bool IsFiat(this TradeInfo tradeInfo)
    {
        return tradeInfo.Offer.PaymentMethodId != PaymentMethodId.BLOCK_CHAINS && tradeInfo.Offer.PaymentMethodId != PaymentMethodId.BLOCK_CHAINS_INSTANT;
    }

    public static bool IsCrypto(this TradeInfo tradeInfo)
    {
        return tradeInfo.Offer.PaymentMethodId == PaymentMethodId.BLOCK_CHAINS || tradeInfo.Offer.PaymentMethodId == PaymentMethodId.BLOCK_CHAINS_INSTANT;
    }
}
