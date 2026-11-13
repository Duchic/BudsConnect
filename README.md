# BudsConnect

Minimalistická česká aplikace pro **Windows 11 / Windows 10 2004+** a **Galaxy Buds3 (SM-R530)**. Nativní C#/.NET 10, WPF.

## Spuštění

Připravená přenosná verze je ve složce `dist/BudsConnect`. Spusť `BudsConnect.exe`; soubory z této složky ponech pohromadě. Vyžaduje .NET 10 Desktop Runtime (na vývojovém počítači je již nainstalovaný).

1. Spáruj Galaxy Buds3 v nastavení Bluetooth Windows a otevři pouzdro.
2. Ukonči jiné aplikace spravující Galaxy Buds, pokud blokují ovládací Bluetooth spojení.
3. Spusť aplikaci. Automaticky hledá spárovaná Galaxy Buds3 a opakuje připojení po osmi sekundách.
4. Pro změnu hlasitosti zvol Galaxy Buds3 jako výchozí zvukový výstup Windows.
5. Přetáhni ikonu BudsConnect z nabídky skrytých ikon vedle hodin. Windows řídí její viditelnost i pořadí.

## Funkce

- Tlačítko **Mini** vedle „Bluetooth ve Windows“ přepne stejné okno do kompaktního režimu (286 × 290 bodů): ikony levého/pravého sluchátka a pouzdra s bateriemi, malý přepínač ANC a hlasitost. **Zvětšit** vrátí původní velikost. Režimy sdílejí připojení i aktuální stav; ANC se zobrazí až po potvrzení sluchátek.

- Skutečné hodnoty baterie levého a pravého sluchátka a pouzdra, pokud je zařízení poskytne.
- Číselná ikona u hodin: nižší dostupná baterie sluchátek; při odpojení pomlčka. Detail po najetí, rychlý panel po kliknutí.
- ANC zapnuto/vypnuto, s potvrzením od sluchátek a chybou při neúspěchu.
- Hlasitost výchozího zvukového výstupu, pouze pokud jde o Galaxy Buds3.
- Světlý/tmavý vzhled nebo vzhled podle Windows.
- Zavření do oznamovací oblasti, volitelné spuštění s Windows, upozornění pod 20 % (znovu až po nabití alespoň na 25 %).
- Pravé tlačítko na ikoně → Ukončit. Automatické spuštění lze vypnout v Nastavení.
- Bez účtu, cloudu a telemetrie aplikace.

## Omezení první verze

Komunikace používá komunitně popsaný protokol, nikoli oficiální Samsung SDK. Testy parseru a sestavení nenahrazují ověření na fyzických sluchátkách. Buds3 Pro a Buds3 FE jsou záměrně vyloučené. Přejmenované zařízení musí mít v názvu „Buds3“ nebo „Buds 3“. Při více spárovaných párech aplikace vyžádá jednoznačné párování. Hodnota pouzdra může být nedostupná při zavřeném pouzdře; zobrazí se „—“. Ovládání zvuku se při změně výchozího výstupu vypne, místo aby měnilo hlasitost reproduktorů.

Nastavení a rotovaný lokální protokol chyb: `%LOCALAPPDATA%\BudsConnect`. Spuštění s Windows používá uživatelský klíč `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\BudsConnect`. Aplikace nevyžaduje administrátora. Před přesunutím přenosné složky vypni automatické spuštění a v novém umístění jej znovu zapni.

## Vývoj a ověření

```powershell
dotnet restore BudsConnect.csproj
dotnet build BudsConnect.csproj -c Release --no-restore
dotnet run --project tests/BudsConnect.Tests.csproj -c Release -- --render
dotnet publish BudsConnect.csproj -c Release --no-restore --self-contained false -o dist/BudsConnect
```

Testovací konzole ověřuje známý CRC vektor, referenční Samsung CRC, fragmentaci dat na každé hranici, sloučené zprávy, poškození a obnovení proudu, délky, hodnoty baterií, nedostupné hodnoty a změny ANC. Volba `--render` ověří pozadí a kontrast všech oken při přepínání motivů a vykreslí je bez zobrazení na ploše do `TestResults/*Window-light.png` a `TestResults/*Window-dark.png`.

### Ruční test na Galaxy Buds3

- Zkontrolovat levé/pravé/pouzdro proti Galaxy Wearable a ověřit pohyb hodnot v ikoně.
- Přepnout ANC oběma směry; ověřit také změnu fyzickým ovládáním sluchátek.
- Zavřít pouzdro, vypnout Bluetooth, znovu připojit a uspat/probudit počítač.
- Vyzkoušet jedno sluchátko, nedostupné pouzdro, upozornění nízké baterie.
- Ověřit rychlý panel, zavření do tray, ukončení a více monitorů s různým DPI.

Zdroje protokolu: [docs/protocol.md](docs/protocol.md). NAudio 2.2.1 je distribuováno pod MIT; licenční podmínky závislostí jsou v jejich balíčcích NuGet.
