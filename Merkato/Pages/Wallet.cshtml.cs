using Haveno.Api.Models;
using Haveno.Api.Services;
using Merkato.Helpers;
using Merkato.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Transactions;

namespace Merkato.Pages;

public class WalletModel : PageModel
{
    private readonly IWalletService _walletService;

    [BindProperty]
    public decimal? Amount { get; set; }
    [BindProperty]
    public string DestinationAddress { get; set; } = string.Empty;
    
    [BindProperty(SupportsGet = true)]
    public int TabIndex { get; set; }
    public Tabs Tabs { get; set; }

    public List<XmrTx> Transactions { get; set; } = [];

    public long Height { get; set; }


    [BindProperty]
    public bool IsSeedModalOpen { get; set; }
    public string? Seed { get; set; }

    [BindProperty]
    public List<string> Metadata { get; set; }

    public decimal Fee { get; set; }
    [BindProperty]
    public bool IsSendModalOpen { get; set; }

    public WalletModel(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task Initialize()
    {
        Tabs = new([ "Receive", "Send", "Transactions" ], TabIndex);
        
        if (TabIndex == 2)
        {
            var transactions = await _walletService.GetXmrTxsAsync();

            Transactions = transactions.ToList();

            var walletHeight = await _walletService.GetHeightAsync();
            Height = walletHeight.Height;
        }
    }

    public async Task OnGetAsync()
    {
        await Initialize();
    }

    public async Task OnPostShowSeedAsync()
    {
        await Initialize();
        Seed = await _walletService.GetXmrSeedAsync();
        IsSeedModalOpen = true;
    }

    public async Task OnPostCreateTransactionAsync()
    {
        if (Amount is null)
            throw new Exception("Amount cannot be null.");

        await Initialize();

        var availableBalance = (await _walletService.GetBalancesAsync()).AvailableBalance;

        IEnumerable<XmrTx> transactions;
        if (Amount == availableBalance.ToMonero())
        {
            transactions = await _walletService.CreateXmrSweepTxsAsync(DestinationAddress);
        }
        else
        {
            var transaction = await _walletService.CreateXmrTxAsync([
                new XmrDestination {
                    Address = DestinationAddress,
                    Amount = Amount.Value.ToPiconero()
                }
            ]);

            transactions = [ transaction ];
        }

        Fee = transactions
            .Aggregate(ulong.MinValue, (x, y) => y.Fee + x)
            .ToMonero();

        IsSendModalOpen = true;
        Metadata = transactions.Select(x => x.Metadata).ToList();
    }

    public async Task OnPostRelayTxAsync()
    {
        await Initialize();
        var txIds = await _walletService.RelayXmrTxsAsync(Metadata);
    }

    public async Task OnPostSetMaxAsync()
    {
        await Initialize();
        var xmrBalanceInfo = await _walletService.GetBalancesAsync();
        Amount = xmrBalanceInfo.AvailableBalance.ToMonero();
    }
}
