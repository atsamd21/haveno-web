using Avalonia;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Merkato.Launcher;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var mutex = new Mutex(true, "Global\\HavenoWeb", out var result);
        if (!result)
        {
            Console.WriteLine("Haveno web is already running.");
            MessageOtherInstance();
            return;
        }

        GC.KeepAlive(mutex);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void MessageOtherInstance()
    {
        using var client = new NamedPipeClientStream(
            ".", 
            "haveno-web-pipe", 
            PipeDirection.Out);

        client.Connect(1_000);

        using var writer = new StreamWriter(client)
        {
            AutoFlush = true
        };

        writer.Write("open");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
