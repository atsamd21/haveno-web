using System;
using System.Diagnostics;
using System.IO;

if (args.Length != 1)
{
    throw new ArgumentException("Script requires a git tag.");
}

if (File.Exists(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "haveno", "daemon", "build", "libs", "daemon-all.jar"))))
{
    Console.WriteLine("Daemon already exists.");
    return;
}

RunCommand("git", ["fetch", "--tags"]);
RunCommand("git", ["checkout", args[0]]);

if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "haveno")))
{
    // Might need to run --recurse-submodules
    throw new Exception("Haveno directory could not be found.");
}

Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "haveno", ".localnet"));

var gradlew = Path.Combine(Directory.GetCurrentDirectory(), "haveno", "gradlew");
if (OperatingSystem.IsWindows())
{
    gradlew += ".bat";
}

RunCommand(gradlew, [":daemon:shadowJar", "-x", "test", "-x", "checkstyleMain", "-x", "checkstyleTest"]);

static void RunCommand(string command, IEnumerable<string> arguments)
{
    var psi = new ProcessStartInfo(command)
    {
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "haveno")
    };

    foreach (var arg in arguments)
    {
        psi.ArgumentList.Add(arg);
    }

    var process = Process.Start(psi);

    process.OutputDataReceived += (_, e) => 
    { 
        if (e.Data is not null) 
            Console.WriteLine(e.Data); 
    };

    process.ErrorDataReceived += (_, e) => 
    { 
        if (e.Data is not null) 
            Console.Error.WriteLine(e.Data); 
    };

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    process!.WaitForExit();

    if (process.ExitCode != 0)
        throw new Exception($"Command \"{command}\" failed with exit code {process.ExitCode}.");
}
