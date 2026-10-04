using System.Reflection;

namespace Merkato.Models;

public static class AppConstants
{
    public static readonly string DaemonUrl;

    public static readonly string SimplexLink;
    public static readonly string GitHubLink;
    public static readonly string MatrixLink;

    public static readonly string ApplicationVersion;

    public const double MakerFeePctTraditional = 0.0015;
    public const double TakerFeePctTraditional = 0.0075;
    public const double MakerFeePctCrypto = 0.0015;
    public const double TakerFeePctCrypto = 0.005;
    public const double PenaltyFeePct = 0.25;
    public const decimal MinimumTradeAmount = 0.05m;
    public const ulong MinSecurityDepositAmountPiconero = 100_000_000_000;

    static AppConstants()
    {
        ApplicationVersion = Assembly.GetExecutingAssembly()
           .GetCustomAttributes<AssemblyMetadataAttribute>()
           .FirstOrDefault(a => a.Key == "ApplicationVersion")?.Value ?? string.Empty;

        DaemonUrl = Assembly.GetExecutingAssembly()
           .GetCustomAttributes<AssemblyMetadataAttribute>()
           .FirstOrDefault(a => a.Key == "DaemonUrl")?.Value ?? string.Empty;

        SimplexLink = Assembly.GetExecutingAssembly()
           .GetCustomAttributes<AssemblyMetadataAttribute>()
           .FirstOrDefault(a => a.Key == "SimplexLink")?.Value ?? string.Empty;

        GitHubLink = Assembly.GetExecutingAssembly()
           .GetCustomAttributes<AssemblyMetadataAttribute>()
           .FirstOrDefault(a => a.Key == "GitHubLink")?.Value ?? string.Empty;

        MatrixLink = Assembly.GetExecutingAssembly()
           .GetCustomAttributes<AssemblyMetadataAttribute>()
           .FirstOrDefault(a => a.Key == "MatrixLink")?.Value ?? string.Empty;
    }
}
