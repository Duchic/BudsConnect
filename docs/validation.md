# Ověření první verze (2026-11-11)

## Mini režim 0.2.0 (2026-11-13)

- Přidáno tlačítko Mini do spodní řady hlavního okna a návrat pomocí Zvětšit. Přepíná se rozhraní i rozměry stejného okna, bez nového Bluetooth připojení.
- Kompaktní rozhraní sdílí baterie, hlasitost, potvrzený ANC, stav připojení a chyby s klasickým oknem. Nedostupný ANC je označen pomlčkou a ovládání se vypne.
- 187 automatických kontrol prošlo, včetně přepnutí rozměrů, návratu do původní velikosti, skutečných click handlerů Mini/Zvětšit/ANC s náhradní službou, baterií, potvrzení ANC a odpojení v obou motivech. Zvukový výstup nebyl při testech měněn.
- Vizuálně ověřeno malé i klasické rozhraní a umístění tlačítka Mini vedle Bluetooth ve Windows. Ošetřeno rozbalení okna u okraje pracovní oblasti monitoru; ruční ověření více monitorů s různým DPI nadále zbývá.

## Oprava příjmu potvrzení ANC 0.1.3 — ověřeno na fyzických Buds3

- Předchozí oprava 0.1.2 nestačila: univerzální ACK sice uměla interpretovat, ale parser ho předtím zahodil kvůli vysokým bitům hlavičky. Skutečné zachycené odpovědi mají hlavičky 0x9005 a 0xD005.
- Živý test před opravou reprodukoval změnu ANC bez rozpoznání potvrzení. Po opravě tentýž test úspěšně změnil původně vypnuté ANC na zapnuté a poté obnovil vypnutý stav. Oba příkazy byly potvrzené skutečnými zprávami 0x42/7801 a 0x42/7800, bez timeoutu. Běžná aplikace byla během testu pozastavena a poté znovu spuštěna.
- 169 automatických kontrol prošlo, včetně obou skutečných rámců rozdělených na každé hranici a všech čtyř kombinací vysokých bitů hlavičky. Zachované kontroly CRC, délky a ukončovacího bajtu.
- Diagnostika je dostupná pouze výslovným spuštěním testovacího programu s `--diagnose-anc`; přepíná ANC a obnovuje původní stav. Běžná aplikace Bluetooth zprávy neukládá. Diagnostika musí mít ovládací spojení volné.

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
