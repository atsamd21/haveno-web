## Haveno Web

**Requires Java 21**

Haveno Web is a server side rendered web UI for <a href="https://github.com/haveno-dex/haveno" target="_blank">Haveno</a>. It does not use any JavaScript and can be accessed either via the Tor browser or locally at localhost:5000.

This repository contains both the razor pages project for the web UI and a small launcher written in Avalonia that manages the process. The application is in beta and has been tested on latest Windows and Debian 13. 

> [!note]
> This repository is configured for the public test network/stagenet. You will need to use a third party mainnet network to make real trades. The developers of this repository do not endorse any networks at this time.

## Tor

The launcher will try to launch the Tor browser from a small number of install locations but is not guaranteed to work.

You can view your onion address on the settings page after starting the app.

## GNOME

If you're using GNOME and want the launcher to appear as a tray icon you will need to install AppIndicator: https://extensions.gnome.org/extension/615/appindicator-support/. It will work without but is a bit more clunky.

## Building from source

Install the .NET 11 SDK 
```bash
https://dotnet.microsoft.com/en-us/download/dotnet/11.0
```

Install a Java 21 SDK
```bash
https://adoptium.net/temurin/releases
```

Clone the repository
```bash
git clone --recursive https://github.com/atsamd21/haveno-web.git
```

Publish
```bash
dotnet publish Merkato.Launcher/Merkato.Launcher.csproj -c Release -r [RUNTIME_IDENTIFIER] --self-contained true -o publish/[RUNTIME_IDENTIFIER]
```

**Valid RIDs: win-x64 and linux-x64, macOS is not supported yet**

## Configuring for different networks

- You can modify appsettings.json to switch network type and further configure the arguments that are passed to the Haveno daemon.

- Update the Haveno submodule so that it points to your Haveno fork.

- Change the HavenoTag property in the .csproj to a tag that exists in your Haveno repo, make sure the actual Haveno version matches.