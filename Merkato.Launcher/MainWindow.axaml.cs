using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

    private async void OnStartClick(object? sender, RoutedEventArgs e)
    {
        _semaphore.Wait();

        try
        {
            SetBothButtonsEnabled(false);

            //var psi = new ProcessStartInfo
            //{
            //    FileName = "Merkato.exe",
            //    WorkingDirectory = "",
            //    UseShellExecute = true,
            //    CreateNoWindow = true
            //};

            //_process = Process.Start(psi);

            await Task.Delay(2000);
        }
        finally
        {
            StartButton.IsVisible = false;
            StopButton.IsVisible = true;

            SetBothButtonsEnabled(true);

            _semaphore.Release();
        }
    }

    private async void OnStopClick(object? sender, RoutedEventArgs e)
    {
        _semaphore.Wait();

        try
        {
            SetBothButtonsEnabled(false);

            await Task.Delay(2000);
        }
        finally
        {
            StartButton.IsVisible = true;
            StopButton.IsVisible = false;

            SetBothButtonsEnabled(true);

            _semaphore.Release();
        }

    }

    private void OnOpenBrowserClick(object? sender, RoutedEventArgs e)
    {

    }
}