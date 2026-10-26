using System.Reflection;

namespace Merkato.Models;

public static class AppConstants
{
    public static readonly string DaemonUrl;

    public static readonly string SimplexLink;
    public static readonly string GitHubLink;
    public static readonly string MatrixLink;

    public static readonly string ApplicationVersion;

    public const decimal MakerFeePctTraditional = 0.0015m;
    public const decimal TakerFeePctTraditional = 0.0075m;
    public const decimal MakerFeePctCrypto = 0.0015m;
    public const decimal TakerFeePctCrypto = 0.005m;
    public const decimal MinimumTradeAmount = 0.05m;
    public const ulong MinSecurityDepositAmountPiconero = 100_000_000_000;

#if DEBUG
    public const bool IsDebug = true;
#else
        public const bool IsDebug = false;
#endif

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
