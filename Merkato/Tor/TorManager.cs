using Haveno.Processes;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Merkato.Tor;

public class TorManager
{
    private readonly string _architecture;
    private readonly string _torDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "merkato", "tor");
    private readonly string _dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "merkato", "tor", "data");

    public TorManager()
    {
        //Directory.CreateDirectory(_torDir);

        if (OperatingSystem.IsWindows())
        {
            _architecture += "windows-";
        }
        else if (OperatingSystem.IsLinux())
        {
            _architecture += "linux-";
        }
        else if (OperatingSystem.IsMacOS()) 
        {
            _architecture += "macos";
            return;
        }
        else
        {
            throw new NotSupportedException("Operating system is not supported.");
        }

        if (RuntimeInformation.OSArchitecture == Architecture.X64)
        {
            _architecture += "x86_64";
        }
        else if (RuntimeInformation.OSArchitecture == Architecture.X86)
        {
            _architecture += "i686";
        }
        else
        {
            throw new NotSupportedException("Architecture is not supported.");
        }
    }

    public void StartTor()
    {
        string executable = "tor";
        if (OperatingSystem.IsWindows())
        {
            executable += ".exe";
        }

        string executablePath = Path.Combine(_torDir, "tor", executable);

        ProcessStartInfo processStartInfo = new(executablePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.Combine(_torDir, "tor"),
            ArgumentList =
            {
                "--torrc-file",
                $"{Path.Combine(_dataDir, "torrc")}"
            }
        };

        var torProcess = ManagedProcess.Start(processStartInfo);
        if (torProcess is null)
            throw new Exception("...");

        var readStdOutTask = Task.Factory.StartNew(async () =>
        {
            string? line;
            while ((line = await torProcess.StandardOutput.ReadLineAsync()) != null)
            {
                Console.WriteLine($"Tor: {line}");
            }
        }, TaskCreationOptions.LongRunning);

        var readStdErrorTask = Task.Factory.StartNew(async () =>
        {
            string? line;
            while ((line = await torProcess.StandardError.ReadLineAsync()) != null)
            {
                Console.WriteLine($"Tor: {line}");
            }
        }, TaskCreationOptions.LongRunning);
    }

    public Version? GetInstalledTorVersion()
    {
        string executable = "tor";
        if (OperatingSystem.IsWindows())
        {
            executable += ".exe";
        }

        string executablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "merkato", "tor", "tor", executable);

        ProcessStartInfo processStartInfo = new(executablePath)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            ArgumentList =
            {
                "--version"
            }
        };

        Process? torProcess;
        try
        {
            torProcess = Process.Start(processStartInfo);
            if (torProcess is null)
                return null;
        }
        catch
        {
            return null;
        }

        string output = torProcess.StandardOutput.ReadToEnd();
        torProcess.WaitForExit();

        Match match = Regex.Match(output, @"Tor version\s+([0-9]+(?:\.[0-9]+)+)");
        if (match.Success)
        {
            return new Version(match.Groups[1].Value);
        }

        return null;
    }

    public Version? GetInstalledBundleVersion()
    {
        var versionFilePath = Path.Combine(_torDir, "version");
        if (!File.Exists(versionFilePath))
            return null;

        var version = File.ReadAllLines(versionFilePath);
        return new Version(version[0]);
    }

    public async Task<Version> GetLatestAvailableVersionAsync()
    {
        using var httpClient = new HttpClient();

        var sources = await httpClient.GetFromJsonAsync<JsonDocument>($"https://aus1.torproject.org/torbrowser/update_3/release/download-{_architecture}.json");
        if (sources is null)
            throw new Exception("...");

        var latestVersion = sources.RootElement.GetProperty("version").GetString()!;
        return new Version(latestVersion);
    }

    public async Task EnsureTorInstalledAsync()
    {
        using var httpClient = new HttpClient();

        var sources = await httpClient.GetFromJsonAsync<JsonDocument>($"https://aus1.torproject.org/torbrowser/update_3/release/download-{_architecture}.json");
        if (sources is null)
            throw new Exception("...");

        var latestBundleVersion = sources.RootElement.GetProperty("version").GetString()!;

        var installedBundleVersion = GetInstalledBundleVersion();
        if (installedBundleVersion is not null)
        {
            var parsed = new Version(latestBundleVersion);
            if (parsed == installedBundleVersion)
            {
                // Up to date
                return;
            }
        }

        string fingerprint = "EF6E286DDA85EA2A4BA7DE684E2C6E8793298290";

        var bytes = await httpClient.GetByteArrayAsync($"https://keys.openpgp.org/vks/v1/by-fingerprint/{fingerprint}");
        using var memoryStream = new MemoryStream(bytes);

        using var decoder = PgpUtilities.GetDecoderStream(memoryStream);
        var pubRingBundle = new PgpPublicKeyRingBundle(decoder);

        var signatureUrl = $"https://dist.torproject.org/torbrowser/{latestBundleVersion}/tor-expert-bundle-{_architecture}-{latestBundleVersion}.tar.gz.asc";
        var signatureBytes = await httpClient.GetByteArrayAsync(signatureUrl);
        using var signatureStream = new MemoryStream(signatureBytes);

        using var sigDecoder = PgpUtilities.GetDecoderStream(signatureStream);
        var pgpObjectFactory = new PgpObjectFactory(sigDecoder);
        var pgpSignatureList = (PgpSignatureList)pgpObjectFactory.NextPgpObject();

        var signature = pgpSignatureList[0];
        PgpPublicKey? publicKey = null;
        foreach (var ring in pubRingBundle.GetKeyRings())
        {
            publicKey = ring.GetPublicKeys().Cast<PgpPublicKey>()
                .FirstOrDefault(k => k.KeyId == signature.KeyId);

            if (publicKey is not null)
                break;
        }

        if (publicKey is null)
            throw new Exception("...");

        signature.InitVerify(publicKey);

        var downloadUrl = $"https://dist.torproject.org/torbrowser/{latestBundleVersion}/tor-expert-bundle-{_architecture}-{latestBundleVersion}.tar.gz";
        var tarBytes = await httpClient.GetByteArrayAsync(downloadUrl);

        signature.Update(tarBytes, 0, tarBytes.Length);

        if (!signature.Verify())
            throw new Exception("Could not verify download.");

        if (Directory.Exists(_torDir))
            Directory.Delete(_torDir, true);

        Directory.CreateDirectory(_torDir);

        using var tarStream = new MemoryStream(tarBytes);

        using var gzip = new GZipStream(tarStream, CompressionMode.Decompress);
        await TarFile.ExtractToDirectoryAsync(gzip, _torDir, overwriteFiles: true);

        // Checks if tor runs
        if (GetInstalledTorVersion() is null)
            throw new Exception("...");

        string hiddenServiceParameter = "HiddenServicePort 80 127.0.0.1:5000";
        string hiddenServiceDirectory = $"HiddenServiceDir {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "merkato", "hidden")}";
        string dataDirectory = "DataDirectory " + _dataDir;
        string geoIpFile = "GeoIPFile " + Path.Combine(_dataDir, "geoip");
        string geoIpv6File = "GeoIPv6File " + Path.Combine(_dataDir, "geoip6");

        var torrcPath = Path.Combine(_torDir, "data", "torrc");
        using var fileStream = File.Open(torrcPath, FileMode.Create, FileAccess.Write);

        using StreamWriter writer = new(fileStream);
        writer.WriteLine(hiddenServiceDirectory);
        writer.WriteLine(hiddenServiceParameter);
        writer.WriteLine(dataDirectory);
        writer.WriteLine(geoIpFile);
        writer.WriteLine(geoIpv6File);
        writer.Close();

        using var versionFileStream = File.Create(Path.Combine(_torDir, "version"));
        using StreamWriter versionFileWriter = new(versionFileStream);
        versionFileWriter.WriteLine(latestBundleVersion);
        versionFileWriter.Close();
    }

    public string GetOnionAddress()
    {
        string hostnameFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "merkato", "hidden", "hostname");
        using var fileStream = File.Open(hostnameFile, FileMode.Open, FileAccess.Read);
        using StreamReader reader = new(fileStream);
        return reader.ReadToEnd();
    }
}
