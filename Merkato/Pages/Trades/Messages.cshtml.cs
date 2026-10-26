using Haveno.Api.Models;
using Haveno.Api.Models.Requests;
using Haveno.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Merkato.Pages.Trades;

public class MessagesModel : PageModel
{
    private readonly ILogger<MessagesModel> _logger;
    private readonly ITradeService _tradeService;
    private readonly IDisputeService _disputeService;

    public TradeInfo TradeInfo { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public bool IsDispute { get; set; }
    [BindProperty]
    public string? Message { get; set; }
    [BindProperty]
    public IFormFile? Attachment { get; set; }

    public MessagesModel(
        ILogger<MessagesModel> logger,
        IDisputeService disputeService,
        ITradeService tradeService)
    {
        _logger = logger;
        _tradeService = tradeService;
        _disputeService = disputeService;
    }

    public async Task Initialize()
    {

    }

    public async Task OnGetAsync(string tradeId)
    {
        TradeInfo = await _tradeService.GetTradeAsync(tradeId);

        if (IsDispute)
        {
            var dispute = await _disputeService.GetDisputeAsync(tradeId);
            ChatMessages = dispute.ChatMessages;
        }
        else
        {
            ChatMessages = (await _tradeService.GetChatMessagesAsync(tradeId)).ToList();
        }


        //HttpContext.Response.Headers.Append("Refresh", "60");
    }

    public async Task OnPostSendMessageAsync(string tradeId)
    {
        TradeInfo = await _tradeService.GetTradeAsync(tradeId);

        if (!string.IsNullOrEmpty(Message))
        {
            await _tradeService.SendChatMessageAsync(tradeId, Message);
        }

        ChatMessages = (await _tradeService.GetChatMessagesAsync(tradeId)).ToList();
    }

    public async Task<IActionResult> OnPostResolveDisputeAsync(string tradeId)
    {
        return LocalRedirect($"/Trades/Messages/{tradeId}?isDispute=true");
    }

    public async Task<IActionResult> OnPostSendDisputeMessageAsync(string tradeId)
    {
        var dispute = await _disputeService.GetDisputeAsync(tradeId);

        var request = new SendDisputeChatMessageRequest 
        {
            DisputeId = dispute.Id,
        };

        if (Attachment is not null)
        {
            using var memoryStream = new MemoryStream();
            using var stream = Attachment.OpenReadStream();

            // TODO config
            if (stream.Length > 400_000)
                throw new Exception("Attachment is too large.");

            stream.CopyTo(memoryStream);

            var bytes = memoryStream.ToArray();
            request.Attachments = [
                new Attachment
                {
                    Bytes = bytes,
                    FileName = Attachment.FileName
                }
            ];
        }

        if (!string.IsNullOrEmpty(Message))
        {
            request.Message = Message;
        }

        await _disputeService.SendDisputeChatMessageAsync(request);

        return LocalRedirect($"/Trades/{tradeId}/Messages?isDispute=true#last-message");
    }
}
