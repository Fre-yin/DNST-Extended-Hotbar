# Changelog

Alle wesentlichen Änderungen an Extended Hotbar werden hier pro veröffentlichter Mod-Version zusammengefasst. MelonLoader- und BepInEx-Ausgaben verwenden dieselbe Versionsnummer und gemeinsame Spiellogik.

## 1.0.2: 10.10.2026

Anpassung an das Spielupdate DS_B.0.4.29 (Steam-Build 25846389), zusammen mit den bisher unveröffentlichten Wartungsänderungen vom 29.09.2026. Unverändert bleiben Spielstandformat, Item-Erweiterung (Version 1), Binding-IDs, Tastenbelegungen, Layout, Rahmengrafik und der DLL-Name `DungeonSettlers10Slots.dll`.

### Spielupdate DS_B.0.4.29

- Neue Fingerabdrücke für Spielcode und Metadaten von Steam-Build 25846389. Auf anderen Spielbuilds schaltet sich die Hotbar weiterhin mit einer Warnung ab. Für DS_B.0.4.23 bleibt 1.0.1 passend.
- Das Spiel benennt Tasten jetzt nach dem Tastaturlayout aus den Spieloptionen (QWERTY, QWERTZ, AZERTY, JIS). Die Hotbar verwendet dafür dieselbe Funktion des Spiels wie dessen eigene Plätze, weil die bisher genutzte Formatierung entfallen ist. Nach einem Layoutwechsel werden die Namen neu gesetzt.
- UI-Prüfmodus: Das Spiel hat den früheren Fehler bei Beschriftungen vor `Awake` selbst behoben. Die Prüfung meldet das jetzt, statt den Fehler vorauszusetzen. Die nachgebaute Optionszeile enthält die neuen Auswahlfelder für Tastaturlayout und Kompatibilität, die das Spiel beim Warten auf eine Taste schließt.

### Mod

- Die Release-DLLs enthalten keinen lokalen Build-Pfad mehr (`DebugType none` und `PathMap` für Release). Beide Paketskripte brechen ab, falls eine DLL doch einen Benutzerpfad enthält.
- Autoren- und Lizenzangaben sind vereinheitlicht. Die MelonLoader-Anzeige lautet „Extended Hotbar v1.0.2 by Fre-yin“.
- Die Harmony-ID der Speicherprüfung ist angepasst. Das Verhalten bleibt gleich.
- Der eingestellte Extended Hotbar Helper ist aus dem Repository entfernt. Der CodeQL-Lauf baut nur noch die Mod und ihre Tests.
- MelonLoader und BepInEx tragen einheitlich Version 1.0.2.

### Prüfung

- Beide Builds ohne Warnungen oder Fehler. Pakettests: MelonLoader 33/33, BepInEx 32/32.
- Steam-Build 25846389 mit BepInEx 6.0.0-be.788 und MelonLoader 0.7.3: Die nativen Prüfläufe bestanden in beiden Loadern. Geprüft wurden zehn Slots, Speichern und Laden mit einer temporären Datei, die Tastaturmatrix, die Gegenstandsplätze, Kontextmenüs mit None, Shift, Ctrl und Alt, die Tastennamen mit Tastaturlayout, zehn Sprachen und die Lebensdauer des Rahmens. BepInEx beendete den Test mit Exitcode 0.
- BepInEx: Kampagne geladen. Die UI-Prüfung der Tastennamen bestand auf allen sechs zusätzlichen Plätzen, und die Hotbar liegt mittig zwischen Figurenfeld und Steuerknöpfen.
- Beide Loader: Kampagnen gemeinsam mit Dungeon Settlers Delvers: Core 0.4.1 und Frieren 0.4.1 geladen. Core erkennt die Ladepatches der Hotbar 1.0.2 als kompatibel, Frieren meldet die Hotbar als kompatibel.

## 1.0.1 — 26.09.2026

Wartungsausgabe nach einem Code-Review. Kein neuer Funktionsumfang. Unverändert bleiben:
- Spielstandformat, Item-Erweiterung (Version 1), Binding-IDs und Tastenbelegungen,
- Layout, Rahmengrafik und unterstützte Spielbuilds,
- der DLL-Name `DungeonSettlers10Slots.dll`.

### Mod

- BepInEx: Bei unbekanntem Spielcode oder abweichenden Metadaten deaktiviert sich die Hotbar wie unter MelonLoader mit einer Warnung, ohne Hooks. Bisher meldete das Plugin diesen erwarteten Fall nach einem Spielupdate als Ladefehler.
- BepInEx: Die Startzeile `Hotbar session: …` mit Prozessnummer und lokalem Spielordner entfällt. Sie diente nur dem nicht mehr gepflegten Hotbar Helper. So enthalten geteilte Logs keinen persönlichen Installationspfad mehr.
- Speichern:
  - Der Abgleich der Kampagnendatei mit dem nativen Schreibaufruf vergleicht normalisierte Pfade, erkennt dieselbe Datei also auch in anderer Schreibweise.
  - Meldet das Spiel ein erfolgreiches Speichern, ohne dass die Itemslot-Zusatzdaten vorbereitet wurden, erscheint jetzt ein Fehlereintrag im Log. Das Speichern gilt dann weiterhin als nicht bestätigt.
- Zusatz-Itemeingabe: Läuft die interne Warteschlange über, werden nur nie zugestellte alte Einträge verworfen. Das Log meldet das einmalig; toter Code wurde entfernt.
- UI-Prüflauf `--ds-run-ui-audits`: Die Rahmenprüfung erwartete 8 statt der tatsächlichen 4 Einheiten Abstand zum Itemslot. Der Fehlschlag ließ nur in diesem Prüfmodus das Rahmenlayout aus, sodass die Itemslots über den Skills 5–7 lagen. Normale Starts waren nicht betroffen.
- Kampf-Zahlenpreset-Prüfung: Die Entwickleraktionen werden über ihre Namen statt über feste Zahlen gefiltert. Das Verhalten bleibt gleich.
- MelonLoader und BepInEx tragen einheitlich Version 1.0.1. Die Versionsangabe der DLLs enthält keinen Git-Commit-Zusatz mehr. Dadurch bricht das BepInEx-Paketskript in einem Git-Klon nicht mehr bei der Versionsprüfung ab.

### Prüfung

- Beide Builds ohne Warnungen oder Fehler erstellt. Pakettests: MelonLoader 33/33, BepInEx 32/32.
- BepInEx 6.0.0-be.788 auf der DS_B.0.4.23-Codebasis: native Prüfläufe einschließlich UI-Prüfungen mit geladener Kampagne bestanden, gemeinsam mit Dungeon Settlers Delvers: Core 0.2.0 und ohne Fehlereinträge.
- MelonLoader 0.7.3: Start gemeinsam mit Dungeon Settlers Delvers: Core 0.2.0 und Frieren 0.2.0; Frieren meldet die Hotbar 1.0.1 als kompatibel.
- Nicht im Spiel ausgelöst: der BepInEx-Pfad für unbekannte Spielbuilds, weil dafür abweichende Spieldateien nötig wären.

## 1.0.0 — 13.09.2026

Erste funktionsvollständige Ausgabe.

### Kompatibilität

- Für Steam-Build 25284551 geprüft; weiterhin kompatibel mit der identischen Spielcodebasis aus Build 25269660.
- Der neue Spielpatch verändert Addressables einschließlich UI-Prefabs und Tabellen, aber nicht `GameAssembly.dll` oder die IL2CPP-Metadaten.
- Kompatibilitätsmeldung und Dokumentation nennen beide anhand des Fingerabdrucks technisch nicht unterscheidbaren, geprüften Builds.

### Mod

- Keine Änderung an Hotbar-, Save-, Binding-, Eingabe-, Layout- oder Asset-Logik gegenüber 0.3.9.
- Zehn Skillplätze plus Basisangriff und drei Item-Quickslots bleiben der abgeschlossene Funktionsumfang.
- MelonLoader und BepInEx tragen einheitlich Version 1.0.0; der jeweilige Loader ist am Paketnamen erkennbar.
- Kein Helper, keine automatische Tastenbelegung und keine QA-Funktion im Mod-Paket.

### Prüfung

- Beide finalen Builds ohne Warnungen oder Fehler erstellt.
- Pakettests: MelonLoader 33/33, BepInEx 32/32.
- Vollständige native Prüfläufe beider finaler 1.0.0-DLLs auf Steam 25284551 bestanden.
- Paketstruktur und Eintragshashes geprüft; keine verschachtelten Archive.

Ausführliche Belege und verbleibende Grenzen stehen in [PATCH-25284551.md](PATCH-25284551.md).

## 0.3.9 — 12.09.2026

- Anpassung und native Prüfung für DS_B.0.4.23 / Steam-Build 25269660.
- Aktualisierte native Eingabesignatur mit erhaltenen Modifiern.
- Details: [PATCH-25269660.md](PATCH-25269660.md).
