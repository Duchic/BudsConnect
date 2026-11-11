# Ověření první verze (2026-11-11)

- Release sestavení: bez chyb a upozornění.
- Automatické testy: CRC proti standardnímu vektoru a publikované Samsung zprávě; každá hranice rozdělení rámce; zprávy po jednotlivých bajtech; více zpráv v jednom čtení; poškozená kontrolní suma; zotavení po chybném rámci; fragmenty; příznak odpovědi; krátký a rozšířený stav; neznámé hodnoty; odpojení; identifikace modelu; srozumitelná chyba obsazeného socketu.
- Načtena okna hlavního přehledu, nastavení a rychlého panelu; hlavní okno vykresleno a vizuálně zkontrolováno. Vykreslovací test odhalil a pomohl opravit obousměrnou vazbu ProgressBar na vlastnost jen pro čtení.
- Zvukový výstup Galaxy Buds3 rozpoznán, přečtena skutečná hlasitost. Hlasitost během testu nebyla měněna.
- Živý Bluetooth test nalezl spárovaná Galaxy Buds3 a jejich RFCOMM službu. Připojení odmítl Windows chybou 0x80072740 (obsazený socket). Na počítači běžel BudsManager. Tento proces nebyl ukončen ani změněn.
- Skutečné hodnoty baterií a změna ANC nejsou dosud hardwarově ověřené. Nutný další test po uvolnění spojení druhou aplikací.
- Automatické spuštění, upozornění při nízké baterii, uspání/probuzení a polohování panelu na monitorech s různým DPI vyžadují ruční ověření.

Testy jsou syntetické s výjimkou referenčního CRC a výše uvedeného živého pokusu. Úspěch testů se nevydává za potvrzení plné kompatibility firmwaru.
