# Changelog

Alle wesentlichen Änderungen an Extended Hotbar werden hier pro veröffentlichter Mod-Version zusammengefasst. MelonLoader- und BepInEx-Ausgaben verwenden dieselbe Versionsnummer und gemeinsame Spiellogik.

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
