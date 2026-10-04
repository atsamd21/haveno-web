using Haveno.Processes;

namespace Merkato.Models;

public class AppSettings
{
    public HavenoDaemonArguments? HavenoDaemonArguments { get; set; }
    public string? HavenoDaemonPath { get; set; }
}
