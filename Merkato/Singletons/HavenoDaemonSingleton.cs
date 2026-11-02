using Haveno.Processes;
using Merkato.Models;
using Microsoft.Extensions.Options;

namespace Merkato.Singletons;

public class HavenoDaemonSingleton : BackgroundService
{
    private readonly AppSettings _appSettings;
    private readonly HavenoDaemon _havenoDaemon;

    public DaemonStatus DaemonStatus 
    {
        get
        {
            return _havenoDaemon.Status;
        }
    }

    public HavenoDaemonSingleton(IOptions<AppSettings> options)
    {
        _appSettings = options.Value;

        _havenoDaemon = new(
            Path.Combine(AppContext.BaseDirectory, "haveno", "daemon-all.jar"),
            _appSettings.HavenoDaemonArguments!);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _havenoDaemon.StartAsync();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _havenoDaemon.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
