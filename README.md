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
dotnet publish BudsConnect.csproj -c Release --no-restore --self-contained false -o dist/BudsConnect
```

Launch `dist/BudsConnect/BudsConnect.exe`. Keep the files in that output folder together. This build uses the .NET 10 Desktop Runtime installed on the computer.

## First connection

1. Pair your Galaxy Buds3 in **Windows Bluetooth settings** and open the charging case.
2. Close other Galaxy Buds management apps if they are using the Bluetooth control connection.
3. Launch **BudsConnect**. It automatically looks for your paired earbuds.
4. To use the volume slider, select **Galaxy Buds3** as the default audio output in Windows.
5. To keep the battery number visible, drag the BudsConnect icon from the hidden-icons menu to the taskbar beside the clock. Windows controls the icon's visibility and position.

Use **Reconnect** to try again immediately. Otherwise, the app retries a lost connection every eight seconds.

## Everyday controls

### Main window and Mini mode

Click **Mini** next to **Windows Bluetooth** to switch to the compact interface. It shows the left and right earbuds and the case, their battery levels, ANC, and volume. Click **Expand** to restore the full window.

Both layouts share the same connection and live state. Switching layouts does not reconnect the earbuds or change their settings.

### System tray

- The number shows the **lower available battery level of the two earbuds**. The case battery is not included.
- Hover over the icon for individual battery levels.
- Click the icon to open the quick-control panel.
- Right-click and choose **Exit** to close the application completely.
- A dash means the earbuds are disconnected or the battery value is unavailable.

Battery and ANC updates arrive from the earbuds; there is no fixed battery polling interval. The volume display refreshes every two seconds. The main window shows when battery data was last received.

### Settings

- **Language:** choose **English** or **Čeština**, then click **Save**. The change applies immediately and is remembered for the next launch.
- **Appearance:** use light mode, dark mode, or the Windows setting.
- **Start with Windows:** launch BudsConnect automatically in the background.
- **Keep running when the window is closed:** close the window without stopping the tray icon and connection.
- **Low-battery notifications:** notify below 20%; another alert is enabled after the battery reaches at least 25%.

**Česky:** V nabídce **Settings → Language** vyber **Čeština** a klikni na **Save**. Přeloží se hlavní okno, Mini režim, rychlý panel, nabídka u hodin i hlášky aplikace.

## Troubleshooting and current limitations

| Symptom | What to check |
| --- | --- |
| Another app is using the connection | Close Galaxy Buds / BudsManager or another earbud manager, then click **Reconnect**. |
| Earbuds are not found | Check pairing and Bluetooth in Windows. A renamed device must still contain `Buds3` or `Buds 3` in its name. |
| More than one pair is found | This version requires a single matching paired Galaxy Buds3 device. |
| The case battery shows a dash | The earbuds may not report the case battery, particularly when the case is closed. |
| The volume slider is unavailable | Set Galaxy Buds3 as the default playback device. The app does not change the volume of unrelated speakers or headphones. |
| An ANC change is not confirmed | Check the connection and try again. The app waits up to five seconds and retains the last confirmed state if the command fails. |

ANC switching and confirmation have been tested on physical Galaxy Buds3. Firmware compatibility, charging transitions, sleep/resume, and mixed-DPI monitor behavior still need broader testing. See the [validation notes](docs/validation.md) for the scope of completed checks.

## Local settings and logs

Settings and rotating error logs are stored in:

```text
%LOCALAPPDATA%\BudsConnect
```

The optional startup entry is stored under:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\BudsConnect
```

Administrator access is not required. If you move the application folder, disable **Start with Windows** first, then enable it again from the new location.

## Development and validation

```powershell
dotnet build BudsConnect.csproj -c Release --no-restore
dotnet run --project tests/BudsConnect.Tests.csproj -c Release -- --render
```

The test harness covers Bluetooth framing and checksums, captured ANC acknowledgements, battery values, connection state, UI synchronization, Mini mode, themes, and language switching. The `--render` option also creates window previews under `TestResults/` without opening them on the desktop.

Further hardware checks should cover battery readings against Galaxy Wearable, one-earbud use, charging-case transitions, low-battery alerts, Bluetooth reconnection, sleep/resume, and multiple monitors with different DPI settings.

## Acknowledgements

- Bluetooth protocol research: [GalaxyBudsClient](https://github.com/timschneeb/GalaxyBudsClient). Implementation references and captured frame details are recorded in [protocol notes](docs/protocol.md).
- Audio integration: [NAudio](https://github.com/naudio/NAudio), distributed under the MIT license. Dependency license terms are included in their NuGet packages.
