# Ověření první verze (2026-11-11)

## Oprava indikace ANC 0.1.2

- Lokální protokol ukazoval opakované vypršení čekání na potvrzení změny ANC. Doplněno univerzální potvrzení 0x42 pro příkaz 0x78: druhý bajt je aplikovaný režim, nikoli návratový kód.
- Aktivní volba je zvýrazněná modrým rámečkem a podkladem; zobrazuje se také „ANC je zapnuté/vypnuté“. Stav se synchronizuje mezi hlavním oknem a rychlým panelem a reaguje na oznámení změny ze sluchátek.
- 145 kontrol prošlo. Testy používají skutečná okna a jejich click handlery s náhradní službou: opakované zapnutí/vypnutí, potvrzení 0x42, vnější změnu, odmítnutí příkazu a odpojení, v obou motivech. Při neúspěchu se zachová poslední potvrzený stav.
- Ověřeny neplatné a nesouvisející potvrzovací zprávy a zachování původní podpory odpovědi 0x51. Komunikace se skutečnými sluchátky nebyla těmito syntetickými testy simulována jako hardwarové ověření.

## Oprava motivů 0.1.1

- Regresní kontrola před opravou selhala: skutečné pozadí MainWindow nepoužívalo aplikační paletu. Implicitní styl Window se neuplatňoval na odvozená okna, zatímco text přebíral tmavé barvy.
- Všechna tři okna nyní explicitně používají společný styl AppWindow. Text dědí barvu svého ovládacího prvku; nativní seznam vzhledu používá odpovídající systémové barvy.
- Nedostupná tlačítka již nesnižují průhlednost textu. Zůstávají neaktivní, ale čitelná.
- 83 kontrol prošlo, včetně přepnutí světlý → tmavý → světlý, skutečného pozadí všech oken a kontrastu textů alespoň 4,5 : 1.
- Vizuálně ověřeno hlavní okno, nastavení a rychlý panel v obou motivech. Vykreslení nyní používá skutečné pozadí okna; předchozí pomocné vyplnění obrázku paletou tuto chybu zakrývalo.

## Původní ověření

- Release sestavení: bez chyb a upozornění.
- Automatické testy: CRC proti standardnímu vektoru a publikované Samsung zprávě; každá hranice rozdělení rámce; zprávy po jednotlivých bajtech; více zpráv v jednom čtení; poškozená kontrolní suma; zotavení po chybném rámci; fragmenty; příznak odpovědi; krátký a rozšířený stav; neznámé hodnoty; odpojení; identifikace modelu; srozumitelná chyba obsazeného socketu.
- Načtena okna hlavního přehledu, nastavení a rychlého panelu; hlavní okno vykresleno a vizuálně zkontrolováno. Vykreslovací test odhalil a pomohl opravit obousměrnou vazbu ProgressBar na vlastnost jen pro čtení.
- Zvukový výstup Galaxy Buds3 rozpoznán, přečtena skutečná hlasitost. Hlasitost během testu nebyla měněna.
- Živý Bluetooth test nalezl spárovaná Galaxy Buds3 a jejich RFCOMM službu. Připojení odmítl Windows chybou 0x80072740 (obsazený socket). Na počítači běžel BudsManager. Tento proces nebyl ukončen ani změněn.
- Skutečné hodnoty baterií a změna ANC nejsou dosud hardwarově ověřené. Nutný další test po uvolnění spojení druhou aplikací.
- Automatické spuštění, upozornění při nízké baterii, uspání/probuzení a polohování panelu na monitorech s různým DPI vyžadují ruční ověření.

Testy jsou syntetické s výjimkou referenčního CRC a výše uvedeného živého pokusu. Úspěch testů se nevydává za potvrzení plné kompatibility firmwaru.
