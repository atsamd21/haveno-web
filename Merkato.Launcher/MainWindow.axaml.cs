using Avalonia.Controls;
using Avalonia.Interactivity;
using Haveno.Processes;
using Merkato.Tor;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Merkato.Launcher;

public partial class MainWindow : Window
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Process? _process;

    public MainWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) => Hide();

        StopButton.IsVisible = false;
    }

    private void SetBothButtonsEnabled(bool enabled)
    {
        StartButton.IsEnabled = enabled;
        StopButton.IsEnabled = enabled;

        if (enabled)
        {
            StartButton.Content = "Start";
            StopButton.Content = "Stop";
        }
        else
        {
            StartButton.Content = "Starting...";
            StopButton.Content = "Stopping...";
        }
    }

    private void OnStartClick(object? sender, RoutedEventArgs e)
    {
        _semaphore.Wait();

        try
        {
            SetBothButtonsEnabled(false);

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(AppContext.BaseDirectory, "Merkato.exe"),
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "UseTor=true"
                }
            };

#if DEBUG
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
#endif

            _process = ManagedProcess.Start(psi);

            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    Debug.WriteLine(e.Data);
            };

            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    Debug.WriteLine(e.Data);
            };

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
        finally
        {
            StartButton.IsVisible = false;
            StopButton.IsVisible = true;

            SetBothButtonsEnabled(true);

            _semaphore.Release();
        }
    }

    private void OnStopClick(object? sender, RoutedEventArgs e)
    {
        _semaphore.Wait();

        try
        {
            SetBothButtonsEnabled(false);

            _process.Kill();
            _process.WaitForExit();
            _process.Dispose();
        }
        finally
        {
            StartButton.IsVisible = true;
            StopButton.IsVisible = false;

            SetBothButtonsEnabled(true);

            _semaphore.Release();
        }
    }

    private string? GetTorBrowserPath()
    {
        string[] paths;

        if (OperatingSystem.IsWindows())
        {
            paths = [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "Tor Browser"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tor Browser"),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs)
               ];
        }
        else
        {
            paths = [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "tor-browser", "Browser"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "applications")
                ];
        }

        string? path;

        foreach (var p in paths)
        {
            if (!Directory.Exists(p))
                continue;

            path = Directory.GetFiles(p, OperatingSystem.IsWindows() ? "*Tor Browser*" : "*start-tor-browser*")
                .FirstOrDefault();

            if (path is not null)
            {
                if (path.Contains(".desktop") && OperatingSystem.IsLinux())
                {
                    foreach (var line in File.ReadLines(path))
                    {
                        if (!line.StartsWith("Exec=", StringComparison.Ordinal)) 
                            continue;

                        var match = Regex.Match(line, "(/[^\"'\\s]*start-tor-browser)(?![.\\w-])");
                        if (match.Success && File.Exists(match.Groups[1].Value))
                            return match.Groups[1].Value;
                    }
                }

                return path;
            }
        }

        return null;
    }

    private void OnOpenBrowserClick(object? sender, RoutedEventArgs e)
    {
        //Process.Start(new ProcessStartInfo
        //{
        //    FileName = "http://localhost:5000",
        //    UseShellExecute = true
        //});

        TorManager torManager = new();

        var address = torManager.GetOnionAddress();

        var torPath = GetTorBrowserPath();
        if (torPath is not null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GetTorBrowserPath(),
                Arguments = $"http://{address}",
                UseShellExecute = true
            });
        }
    }
}