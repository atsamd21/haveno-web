using Haveno.Api.Models;
using Haveno.Api.Services;

namespace Merkato.Helpers;

public static class OfferHelper
{
    public static double GetTakerDepositPercent(this OfferInfo offerInfo)
    {
        double securityDepositPercent;

        if (offerInfo.Direction == OfferDirection.BUY)
            securityDepositPercent = offerInfo.SellerSecurityDepositPercent;
        else
            securityDepositPercent = offerInfo.BuyerSecurityDepositPercent;

        ulong depositAmount = (ulong)(offerInfo.Amount * securityDepositPercent);
        if (depositAmount < 100_000_000_000)
        {
            securityDepositPercent = 100_000_000_000 / (double)offerInfo.Amount;
        }

        return securityDepositPercent;
    }

    public static ulong GetTakerDepositAmount(this OfferInfo offerInfo)
    {
        return (ulong)(offerInfo.Amount * offerInfo.GetTakerDepositPercent());
    }

    public static async Task<decimal> GetFixedPricePercentageAsync(this OfferInfo offerInfo, IPriceService priceService)
    {
        if (offerInfo.UseMarketBasedPrice)
            throw new Exception("...");

        try
        {
            var price = await priceService.GetMarketPriceAsync(offerInfo.CounterCurrencyCode);

            if (offerInfo.Direction == OfferDirection.BUY)
            {
                return 1m - (offerInfo.Price / price);
            }
            else
            {
                return (offerInfo.Price / price) - 1m;
            }
        }
        catch
        {
            return 0m;
        }
    }
}
