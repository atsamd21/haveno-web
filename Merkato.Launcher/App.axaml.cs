using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace Merkato.Launcher;

public partial class App : Application
{
    private MainWindow? _window;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        _window ??= new MainWindow();

        if (_window.IsVisible)
        {
            _window.Hide();
            return;
        }

        var screen = _window.Screens.Primary;
        if (screen is null) 
            return;

        var screenArea = screen.WorkingArea;
        var windowWidth = (int)(_window.Width * screen.Scaling);
        var windowHeight = (int)(_window.Height * screen.Scaling);

        _window.Position = new PixelPoint(
            screenArea.Width - windowWidth - 20, 
            screenArea.Height - windowHeight - 20);

        _window.Show();
        _window.Activate();
    }

    private void OnQuit(object? sender, EventArgs e) => (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
}