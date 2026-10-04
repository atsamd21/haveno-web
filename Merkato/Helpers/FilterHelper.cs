using Haveno.Api.Models;

namespace Merkato.Helpers;

public enum PaymentAccountFilter
{
    Traditional,
    Crypto,
    Other
}

public static class FilterHelper
{
    public static IEnumerable<PaymentAccount> FilterCryptoOrTraditionalPaymentAccounts(IEnumerable<PaymentAccount> paymentAccounts, PaymentAccountFilter filter)
    {
        switch (filter)
        {
            case PaymentAccountFilter.Traditional:
                return paymentAccounts
                    .Where(x => x.PaymentMethod.Id != PaymentMethodId.BLOCK_CHAINS && x.PaymentMethod.Id != PaymentMethodId.BLOCK_CHAINS_INSTANT);
            case PaymentAccountFilter.Crypto:
                return paymentAccounts
                    .Where(x => x.PaymentMethod.Id == PaymentMethodId.BLOCK_CHAINS || x.PaymentMethod.Id == PaymentMethodId.BLOCK_CHAINS_INSTANT);
            default: throw new NotImplementedException();
        }
    }
}
