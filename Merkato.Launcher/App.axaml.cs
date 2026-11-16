using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Merkato.Launcher;

public partial class App : Application
{
    private MainWindow? _window;

    private NamedPipeServerStream? _server;

    private bool? _platformSupportsTrayIcons;
    public bool PlatformSupportsTrayIcons
    {
        get
        {
            if (_platformSupportsTrayIcons is null)
            {
                if (!OperatingSystem.IsLinux())
                    return true;

                var processStartInfo = new ProcessStartInfo("gdbus")
                {
                    ArgumentList =
                    {
                        "call",
                        "--session",
                        "--dest",
                        "org.freedesktop.DBus",
                        "--object-path",
                        "/org/freedesktop/DBus",
                        "--method",
                        "org.freedesktop.DBus.NameHasOwner",
                        "org.kde.StatusNotifierWatcher"
                    },
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using var process = Process.Start(processStartInfo)!;
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                _platformSupportsTrayIcons = output.Contains("true");
            }

            return _platformSupportsTrayIcons.Value;
        }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (PlatformSupportsTrayIcons)
            {
                _window = new MainWindow();
            }
            else
            {
                _window = new MainWindow
                {
                    Topmost = false,
                    ShowInTaskbar = true,
                    WindowDecorations = WindowDecorations.Full,
                    WindowStartupLocation = WindowStartupLocation.Manual
                };
            }

            var screen = _window.Screens.Primary;
            if (screen is not null)
            {
                var screenArea = screen.WorkingArea;
                var windowWidth = (int)(_window.Width * screen.Scaling);
                var windowHeight = (int)(_window.Height * screen.Scaling);

                _window.Position = new PixelPoint(
                    screenArea.Width - windowWidth - 20,
                    screenArea.Height - windowHeight - 20);
            }

            desktop.MainWindow = _window;

            ListenForOpenMessage(() =>
            {
                if (desktop.MainWindow.WindowState == WindowState.Minimized) 
                {
                    desktop.MainWindow.WindowState = WindowState.Normal;
                }

                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ListenForOpenMessage(Action action)
    {
        _server = new NamedPipeServerStream(
            "haveno-web-pipe",
            PipeDirection.In,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await _server.WaitForConnectionAsync();

                    using var reader = new StreamReader(_server, leaveOpen: true);

                    var line = await reader.ReadLineAsync();
                    if (line == "open")
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(action);
                    }
                }
                catch
                {

                }
                finally
                {
                    try
                    {
                        _server.Disconnect();
                    }
                    catch
                    {

                    }
                }
            }
        });
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        if (_window is not null)
        {
            _window.Show();
            _window.Activate();
        }
    }

    private void OnQuit(object? sender, EventArgs e) => (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
}