using Haveno.Api.Models;
using Haveno.Api.Services;

namespace Merkato.Helpers;

public static class OfferHelper
{
    public static decimal GetTakerDepositPercent(this OfferInfo offerInfo, ulong piconeroAmount)
    {
        if (offerInfo.BuyerSecurityDepositPercent == 0)
            return 0m;

        decimal securityDepositPercent;

        if (offerInfo.Direction == OfferDirection.BUY)
            securityDepositPercent = (decimal)offerInfo.SellerSecurityDepositPercent;
        else
            securityDepositPercent = (decimal)offerInfo.BuyerSecurityDepositPercent;

        ulong depositAmount = (ulong)(piconeroAmount * securityDepositPercent);
        if (depositAmount < 100_000_000_000m)
        {
            securityDepositPercent = 100_000_000_000m / piconeroAmount;
        }

        return securityDepositPercent;
    }

    public static decimal GetMakerDepositPercent(this OfferInfo offerInfo, ulong piconeroAmount)
    {
        decimal securityDepositPercent;

        if (offerInfo.Direction == OfferDirection.BUY)
            securityDepositPercent = (decimal)offerInfo.BuyerSecurityDepositPercent;
        else
            securityDepositPercent = (decimal)offerInfo.SellerSecurityDepositPercent;

        ulong depositAmount = (ulong)(piconeroAmount * securityDepositPercent);
        if (depositAmount < 100_000_000_000m)
        {
            securityDepositPercent = 100_000_000_000m / piconeroAmount;
        }

        return securityDepositPercent;
    }

    public static ulong GetTakerDepositAmountPiconero(this OfferInfo offerInfo, ulong piconeroAmount)
    {
        return (ulong)(piconeroAmount * offerInfo.GetTakerDepositPercent(piconeroAmount));
    }

    public static ulong GetMakerDepositAmountPiconero(this OfferInfo offerInfo, ulong piconeroAmount)
    {
        return (ulong)(piconeroAmount * offerInfo.GetMakerDepositPercent(piconeroAmount));
    }

    public static decimal GetTakerDepositAmount(this OfferInfo offerInfo, decimal moneroAmount)
    {
        return moneroAmount * offerInfo.GetTakerDepositPercent(moneroAmount.ToPiconero());
    }

    public static decimal GetMakerDepositAmount(this OfferInfo offerInfo, decimal moneroAmount)
    {
        return moneroAmount * offerInfo.GetMakerDepositPercent(moneroAmount.ToPiconero());
    }

    public static ulong GetMakerFeeAmountPiconero(this OfferInfo offerInfo, ulong piconeroAmount)
    {
        return (ulong)(piconeroAmount * offerInfo.MakerFeePercent);
    }

    public static ulong GetTakerFeeAmountPiconero(this OfferInfo offerInfo, ulong piconeroAmount)
    {
        return (ulong)(piconeroAmount * offerInfo.TakerFeePercent);
    }

    public static decimal GetMakerFeeAmount(this OfferInfo offerInfo, decimal moneroAmount)
    {
        return moneroAmount * offerInfo.MakerFeePercent;
    }

    public static decimal GetTakerFeeAmount(this OfferInfo offerInfo, decimal moneroAmount)
    {
        return moneroAmount * offerInfo.TakerFeePercent;
    }

    public static ulong GetTakerTotalRequiredAmountPiconero(this OfferInfo offerInfo, ulong piconeroAmount)
    {
        var totalAmountPiconero = offerInfo.GetTakerDepositAmountPiconero(piconeroAmount) + offerInfo.GetTakerFeeAmountPiconero(piconeroAmount);
        if (offerInfo.Direction == OfferDirection.BUY)
        {
            totalAmountPiconero += piconeroAmount;
        }

        return totalAmountPiconero;
    }

    public static decimal GetTakerTotalRequiredAmount(this OfferInfo offerInfo, decimal moneroAmount)
    {
        var totalAmount = offerInfo.GetTakerDepositAmount(moneroAmount) + offerInfo.GetTakerFeeAmount(moneroAmount);
        if (offerInfo.Direction == OfferDirection.BUY)
        {
            totalAmount += moneroAmount;
        }

        return totalAmount;
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

    public static async Task<decimal> GetPricePercentageAsync(this OfferInfo offerInfo, IPriceService priceService)
    {
        if (offerInfo.UseMarketBasedPrice)
        {
            return (decimal)offerInfo.MarketPriceMarginPercent;
        }

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

    public static bool IsFiat(this OfferInfo offerInfo) 
    {
        return offerInfo.PaymentMethodId != PaymentMethodId.BLOCK_CHAINS && offerInfo.PaymentMethodId != PaymentMethodId.BLOCK_CHAINS_INSTANT;
    }

    public static bool IsCrypto(this OfferInfo offerInfo)
    {
        return offerInfo.PaymentMethodId == PaymentMethodId.BLOCK_CHAINS || offerInfo.PaymentMethodId == PaymentMethodId.BLOCK_CHAINS_INSTANT;
    }

    //public static ulong CalculateMaxSpendableFundsAsync(this OfferInfo offerInfo, decimal availableFundsPiconero)
    //{
    //    if (offerInfo.IsMyOffer)
    //        throw new Exception("Cannot calculate max spendable on my offer.");

    //    if (availableFundsPiconero == 0)
    //        return 0;

    //}
}
