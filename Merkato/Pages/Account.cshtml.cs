using Haveno.Api.Models;
using Haveno.Api.Models.Requests;
using Haveno.Api.Services;
using Merkato.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Transactions;

namespace Merkato.Pages;

public class AccountModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly IPaymentAccountService _paymentAccountService;

    public List<PaymentMethod> PaymentMethods { get; set; } = [];
    public List<PaymentAccount> PaymentAccounts { get; set; } = [];

    [BindProperty]
    public PaymentAccountForm? PaymentAccountForm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PaymentMethodId { get; set; }
    [BindProperty(SupportsGet = true)]
    public int TabIndex { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PaymentAccountId { get; set; }
    public PaymentAccount? PaymentAccount { get; set; }

    public Tabs Tabs { get; set; }

    public AccountModel(
        IAccountService accountService, 
        IPaymentAccountService paymentAccountService)
    {
        _paymentAccountService = paymentAccountService;
        _accountService = accountService;
    }

    public async Task Initialize()
    {
        Tabs = new(["Traditional", "Crypto"], TabIndex);
    }

    public async Task OnGetAsync()
    {
        await Initialize();
    }

    public async Task OnPostDeleteAccountAsync(string accountId)
    {
        await Initialize();
        await _paymentAccountService.DeletePaymentAccountAsync(accountId);
    }

    public async Task OnPostCreateAccountAsync()
    {
        await Initialize();

        foreach (var field in PaymentAccountForm.Fields)
        {
            if (field.Values.Count != 0)
            {
                field.Value = string.Join(',', field.Values);
            }
        }

        var paymentAccount = await _paymentAccountService.CreatePaymentAccountAsync(new CreatePaymentAccountRequest
        {
            PaymentAccountForm = PaymentAccountForm
        });

        PaymentAccountForm = null;
    }
}
