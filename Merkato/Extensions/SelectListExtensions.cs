using Haveno.Api.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Merkato.Extensions;

public static class SelectListExtensions
{
    public static SelectList CreateSelectListForPaymentAccountFormField(PaymentAccountFormField paymentAccountFormField)
    {
        if (paymentAccountFormField.SupportedCurrencies.Count > 0)
        {
            if (paymentAccountFormField.SupportedCurrencies.First().CryptoCurrency is not null)
            {
                return new(paymentAccountFormField.SupportedCurrencies, nameof(TradeCurrency.Code), nameof(TradeCurrency.Name));
            }
            else
            {
                return new(paymentAccountFormField.SupportedCurrencies, nameof(TradeCurrency.Code), nameof(TradeCurrency.Name));
            }
        }
        else if (paymentAccountFormField.SupportedCountries.Count > 0)
        {
            return new(paymentAccountFormField.SupportedCountries, nameof(Country.Code), nameof(Country.Name));
        }
        else if (paymentAccountFormField.SupportedSepaEuroCountries.Count > 0)
        {
            return new(paymentAccountFormField.SupportedSepaEuroCountries, nameof(Country.Code), nameof(Country.Name));
        }
        else if (paymentAccountFormField.SupportedSepaNonEuroCountries.Count > 0)
        {
            return new(paymentAccountFormField.SupportedSepaNonEuroCountries, nameof(Country.Code), nameof(Country.Name));
        }
        else if (paymentAccountFormField.RequiredForCountries.Count > 0)
        {
            return new(paymentAccountFormField.RequiredForCountries);
        }
        else if (paymentAccountFormField.SupportedValues.Count > 0)
        {
            return new(paymentAccountFormField.SupportedValues);
        }
        else
        {
            throw new Exception();
        }
    }
}
