# BudsConnect

**A minimalist Windows companion for Samsung Galaxy Buds3.**

Check your earbud batteries beside the Windows clock, switch noise cancellation, and adjust volume from a clean desktop interface. Switch to **Mini mode** when you only need the essentials.

Built with **C# · .NET 10 · WPF**. Available in **English and Czech**.

## Features

| Feature | What it does |
| --- | --- |
| Battery overview | Shows separate battery levels for the left earbud, right earbud, and charging case when available. |
| Battery beside the clock | Displays the lower available earbud battery level as a number in the Windows system tray. Hover for details or click for quick controls. |
| Noise cancellation | Switches ANC on or off and updates the interface when the earbuds confirm the change. |
| Volume control | Adjusts the Windows audio output when Galaxy Buds3 are the default playback device. |
| Mini mode | Shrinks the main window to battery icons, an ANC switch, a volume slider, and an **Expand** button. |
| Light and dark themes | Choose a theme or follow the Windows appearance setting. |
| English and Czech | Change the language in Settings without restarting the app. English is the default. |
| Background operation | Keep the app running in the system tray and optionally start it with Windows. |
| Low-battery alerts | Receive a notification when an earbud battery drops below 20%. |

No account, cloud service, or application telemetry is required. Settings and error logs stay on your PC.

## Compatibility

- **Earbuds:** Samsung Galaxy Buds3 (**SM-R530**).
- **Windows:** Windows 11 or Windows 10 version 2004 and later.
- **Bluetooth:** Earbuds must already be paired with Windows.
- **Runtime:** .NET 10 Desktop Runtime for the framework-dependent build.
- **Development:** .NET 10 SDK on Windows.

Galaxy Buds3 Pro and Galaxy Buds3 FE are not supported by this version. BudsConnect uses a community-documented Bluetooth protocol rather than an official Samsung SDK.

## Build and run

Clone the repository and run these commands from its root directory on Windows:

```powershell
git clone https://github.com/Duchic/BudsConnect.git
cd BudsConnect
dotnet restore BudsConnect.csproj
dotnet build BudsConnect.csproj -c Release --no-restore
dotnet run --project tests/BudsConnect.Tests.csproj -c Release -- --render
```

The test harness covers Bluetooth framing and checksums, captured ANC acknowledgements, battery values, connection state, UI synchronization, Mini mode, themes, and language switching. The `--render` option also creates window previews under `TestResults/` without opening them on the desktop.

Further hardware checks should cover battery readings against Galaxy Wearable, one-earbud use, charging-case transitions, low-battery alerts, Bluetooth reconnection, sleep/resume, and multiple monitors with different DPI settings.

## Acknowledgements

- Bluetooth protocol research: [GalaxyBudsClient](https://github.com/timschneeb/GalaxyBudsClient). Implementation references and captured frame details are recorded in [protocol notes](docs/protocol.md).
- Audio integration: [NAudio](https://github.com/naudio/NAudio), distributed under the MIT license. Dependency license terms are included in their NuGet packages.
