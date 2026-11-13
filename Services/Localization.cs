using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BudsConnect.Services;

public static class Localization
{
    public sealed record Entry(string Key, string Cs, string En);
    public static IReadOnlyList<Entry> Entries { get; } = new Entry[]
    {
        new("Settings", "Nastavení", "Settings"),
        new("SettingsTitle", "Nastavení · BudsConnect", "Settings · BudsConnect"),
        new("DeviceAlias", "Název zařízení v aplikaci", "Device name in this app"),
        new("DeviceAliasHint", "Platí jen pro BudsConnect. Prázdné pole obnoví původní název sluchátek.", "Only used in BudsConnect. Leave blank to use the original earbud name."),
        new("NamedTrayTitle", "{0} · rychlé ovládání", "{0} · quick controls"),
        new("NamedLowTitle", "{0} · nízká baterie", "{0} · low battery"),
        new("NamedTrayDisconnected", "{0} · nepřipojeno", "{0} · disconnected"),
        new("NamedTrayDetails", "{0} · L {1} · P {2} · Pouzdro {3}", "{0} · L {1} · R {2} · Case {3}"),
        new("Left", "Levé", "Left"), new("Right", "Pravé", "Right"), new("Case", "Pouzdro", "Case"),
        new("LeftShort", "L", "L"), new("RightShort", "P", "R"),
        new("LeftBud", "Levé sluchátko", "Left earbud"), new("RightBud", "Pravé sluchátko", "Right earbud"),
        new("ChargingCase", "Nabíjecí pouzdro", "Charging case"),
        new("LeftBattery", "Baterie levého sluchátka", "Left earbud battery"),
        new("RightBattery", "Baterie pravého sluchátka", "Right earbud battery"),
        new("CaseBattery", "Baterie pouzdra", "Case battery"),
        new("NoiseControl", "Potlačení hluku", "Noise cancellation"),
        new("Off", "Vypnuto", "Off"), new("On", "Zapnuto", "On"),
        new("AncOff", "ANC vypnuto", "ANC off"), new("ToggleAnc", "Přepnout ANC", "Toggle ANC"),
        new("AncOnStatus", "ANC je zapnuté.", "ANC is on."), new("AncOffStatus", "ANC je vypnuté.", "ANC is off."),
        new("AncWaiting", "Čekám na potvrzení sluchátek…", "Waiting for the earbuds to confirm…"),
        new("AncUnavailableHint", "Ovládání bude dostupné po načtení stavu sluchátek.", "Controls will be available once the earbud status is received."),
        new("Volume", "Hlasitost", "Volume"), new("BudsVolume", "Hlasitost Galaxy Buds3", "Galaxy Buds3 volume"),
        new("VolumeHint", "Ve Windows vyber Galaxy Buds3 jako zvukový výstup.", "Select Galaxy Buds3 as the audio output in Windows."),
        new("VolumeError", "Nejdřív ve Windows zvol Galaxy Buds3 jako zvukový výstup.", "First select Galaxy Buds3 as the audio output in Windows."),
        new("Updated", "Poslední údaj {0}", "Last update {0}"),
        new("BatteryWaiting", "Čekám na údaje baterie", "Waiting for battery data"),
        new("Reconnect", "Znovu připojit", "Reconnect"), new("WindowsBluetooth", "Bluetooth ve Windows", "Windows Bluetooth"),
        new("MiniHint", "Přepnout do malého rozhraní", "Switch to mini mode"),
        new("Expand", "Zvětšit", "Expand"), new("ExpandHint", "Vrátit klasické rozhraní", "Return to the full interface"),
        new("Connected", "Připojeno", "Connected"), new("Disconnected", "Nepřipojeno", "Disconnected"),
        new("Open", "Otevřít BudsConnect", "Open BudsConnect"), new("Exit", "Ukončit", "Exit"),
        new("TrayTitle", "Galaxy Buds3 · rychlé ovládání", "Galaxy Buds3 · quick controls"),
        new("Startup", "Spouštět s Windows", "Start with Windows"),
        new("CloseToTray", "Po zavření běžet na pozadí", "Keep running when the window is closed"),
        new("LowAlert", "Upozornit při baterii pod 20 %", "Notify when battery is below 20%"),
        new("Appearance", "Vzhled", "Appearance"), new("AppAppearance", "Vzhled aplikace", "App appearance"),
        new("System", "Podle Windows", "Use Windows setting"), new("Light", "Světlý", "Light"), new("Dark", "Tmavý", "Dark"),
        new("Language", "Jazyk", "Language"), new("Save", "Uložit", "Save"),
        new("TrayExplanation", "Číslo u hodin ukazuje nižší dostupnou baterii sluchátek. Baterie pouzdra se do něj nezapočítává.", "The number beside the clock shows the lower available earbud battery level. The case battery is not included."),
        new("PinExplanation", "Pro trvalé zobrazení přetáhni ikonu BudsConnect z nabídky skrytých ikon na hlavní panel.", "To keep BudsConnect visible, drag its icon from the hidden icons menu onto the taskbar."),
        new("SaveError", "Nastavení se nepodařilo uložit: {0}", "Could not save settings: {0}"),
        new("AlreadyRunning", "BudsConnect už běží. Otevři jej kliknutím na ikonu u hodin.", "BudsConnect is already running. Open it using the icon beside the clock."),
        new("AppError", "V aplikaci nastala chyba. Podrobnosti jsou v %LOCALAPPDATA%\\BudsConnect\\app.log.", "An application error occurred. Details are in %LOCALAPPDATA%\\BudsConnect\\app.log."),
        new("WindowsSettings", "Nastavení Windows", "Windows settings"),
        new("LowTitle", "Galaxy Buds3 · nízká baterie", "Galaxy Buds3 · low battery"),
        new("LowBody", "Sluchátkům zbývá {0} % baterie.", "Your earbuds have {0}% battery remaining."),
        new("TrayDisconnected", "BudsConnect · nepřipojeno", "BudsConnect · disconnected"),
        new("TrayDetails", "BudsConnect · L {0} · P {1} · Pouzdro {2}", "BudsConnect · L {0} · R {1} · Case {2}"),
        new("AncUnavailable", "Ovládání ANC nyní není dostupné.", "ANC controls are currently unavailable."),
        new("AncRejected", "Sluchátka změnu ANC odmítla.", "The earbuds rejected the ANC change."),
        new("AncTimeout", "Sluchátka nepotvrdila změnu ANC. Zkus to znovu.", "The earbuds did not confirm the ANC change. Please try again."),
        new("Searching", "Hledám spárovaná Galaxy Buds3…", "Looking for paired Galaxy Buds3…"),
        new("NotPaired", "Galaxy Buds3 nejsou spárovaná. Přidej je v nastavení Bluetooth Windows.", "Galaxy Buds3 are not paired. Add them in Windows Bluetooth settings."),
        new("MultipleDevices", "Nalezeno více Galaxy Buds3. Pro první připojení ponech spárovaný jen jeden pár.", "Multiple Galaxy Buds3 devices found. Keep only one pair paired for the first connection."),
        new("AccessDenied", "Windows nepovolil přístup ke sluchátkům.", "Windows denied access to the earbuds."),
        new("Connecting", "Připojuji ke sluchátkům…", "Connecting to the earbuds…"),
        new("DeviceUnavailable", "Sluchátka nejsou dostupná. Otevři pouzdro a ukonči jiné aplikace pro Galaxy Buds.", "The earbuds are unavailable. Open the case and close other Galaxy Buds apps."),
        new("ConnectedWaiting", "Připojeno · čekám na údaje sluchátek", "Connected · waiting for earbud data"),
        new("ConnectionEnded", "Sluchátka ukončila spojení.", "The earbuds closed the connection."),
        new("ConnectedBluetooth", "Připojeno · Bluetooth", "Connected · Bluetooth"),
        new("Retrying", "Spojení přerušeno nebo vypršel čas. Zkusím se znovu připojit…", "Connection interrupted or timed out. Reconnecting…"),
        new("ConnectionLost", "Spojení se sluchátky bylo přerušeno.", "The connection to the earbuds was lost."),
        new("ConnectionBusy", "Spojení používá jiná aplikace. Ukonči Galaxy Buds / BudsManager a klikni na Znovu připojit.", "Another app is using the connection. Close Galaxy Buds / BudsManager and click Reconnect."),
        new("BluetoothDenied", "Windows nepovolil přístup k Bluetooth. Zkontroluj oprávnění a nastavení Bluetooth.", "Windows denied Bluetooth access. Check permissions and Bluetooth settings."),
        new("BluetoothUnavailable", "Bluetooth není dostupné. Zkontroluj Bluetooth ve Windows a otevři pouzdro.", "Bluetooth is unavailable. Check Bluetooth in Windows and open the case."),
        new("NotConnected", "Sluchátka nejsou připojena.", "The earbuds are not connected.")
    };
    public static string Language { get; private set; } = "en";
    public static event Action? Changed;
    public static string Normalize(string? language) => language == "cs" ? "cs" : "en";
    public static string Get(string key, params object[] args)
    {
        var entry = Entries.First(e => e.Key == key);
        string text = Language == "cs" ? entry.Cs : entry.En;
        return args.Length == 0 ? text : string.Format(text, args);
    }
    // Service statuses remain stable message identifiers; resolve at the UI boundary,
    // including errors/status received before a language switch.
    public static string Translate(string message)
    {
        var entry = Entries.FirstOrDefault(e => e.Cs == message || e.En == message);
        return entry == null ? message : Get(entry.Key);
    }
    public static void Apply(string? language)
    {
        Language = Normalize(language);
        foreach (var entry in Entries) Application.Current.Resources["Loc_" + entry.Key] = Get(entry.Key);
        Changed?.Invoke();
    }
}
