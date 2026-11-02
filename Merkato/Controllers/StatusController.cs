using Haveno.Api.Services;
using Merkato.Singletons;
using Microsoft.AspNetCore.Mvc;

namespace Merkato.Controllers;

[Route("api/status")]
[ApiController]
public class StatusController : ControllerBase
{
    private readonly HavenoDaemonSingleton _havenoDaemonSingleton;
    private readonly IWalletService _walletService;

    public StatusController(HavenoDaemonSingleton havenoDaemonSingleton, IWalletService walletService)
    {
        _havenoDaemonSingleton = havenoDaemonSingleton;
        _walletService = walletService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        switch (_havenoDaemonSingleton.DaemonStatus) 
        {
            case Haveno.Processes.DaemonStatus.InitTor:
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Initializing tor" });
            case Haveno.Processes.DaemonStatus.InitWallet:
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Initializing wallet" });
            case Haveno.Processes.DaemonStatus.SyncWallet:
                var height = await _walletService.GetHeightAsync();
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = $"Syncing wallet at block: {height.Height} / {height.TargetHeight}" });
            case Haveno.Processes.DaemonStatus.Running:
                return Ok();
            default:
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Offline" } );
        }
    }
}
