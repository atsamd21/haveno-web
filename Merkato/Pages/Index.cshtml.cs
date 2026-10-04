using Haveno.Api.Models;
using Haveno.Api.Services;
using Merkato.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ITradeStatisticsService _tradeStatisticsService;

    public Modal? Modal { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; }

    public int PageSize { get; set; } = 4;
    public int PageCount { get; set; }

    public bool IsToggled { get; set; }

    public List<TradeStatistic> TradeStatistics 
    { 
        get; 
        private set 
        { 
            field = value; 
            PageCount = TradeStatistics.Count / PageSize; 
        } 
    } = [];

    [BindProperty(SupportsGet = true)]
    public int TabIndex { get; set; }

    [BindProperty(SupportsGet = true)]
    public string TestValue { get; set; } = "test1";

    public Dictionary<string, string> TestValues { get; set; } = new Dictionary<string, string> { 
        { "test1", "test 1" } ,
        { "test2", "test 2" } ,
        { "test3", "test 3" },
        { "test4", "test 4" },
        { "test5", "test 5" },
        { "test6", "test 6" },
        { "test7", "test 7" },
    };

    public IndexModel(ILogger<IndexModel> logger, 
        ITradeStatisticsService tradeStatisticsService)
    {
        _logger = logger;
        _tradeStatisticsService = tradeStatisticsService;
    }

    public List<TradeStatistic> FilterTradeStatistics()
    {
        return TradeStatistics
            .OrderByDescending(x => x.Date)
            .Skip(CurrentPage * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public async Task OnPostTestAsync()
    {

    }

    private async Task Initialize()
    {
        TradeStatistics = (await _tradeStatisticsService.GetTradeStatisticsAsync()).ToList();

        //TradeStatistics = [
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    new TradeStatistic(),
        //    ];
    }

    public async Task OnGetAsync()
    {
        await Initialize();
    }

    public async Task OnPostTestAsync(Test test)
    {
        await Initialize();
    }

    public async Task OnPostModalAsync()
    {
        await Initialize();
    }

    public async Task OnPostOpenModalAsync()
    {
        await Initialize();
        Modal = new("test title", "Modal");
    }
}
