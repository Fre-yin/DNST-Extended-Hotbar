# Extended Hotbar & Hotbar Helper

**Deutsch** | [English](README.en.md)

Zehn aktive Fähigkeitsplätze, ein separater Basisangriff und drei Gegenstandsplätze für Dungeon Settlers. Der optionale Helper erleichtert Installation, lokale Updates und Tastenbelegung.

**Vorabversion: Helper 0.1.12 · MelonLoader-Mod 0.3.8 · BepInEx-Mod 0.3.8-bepinex.1**

## Downloads

Öffne [Releases](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases) und wähle die Assets des Releases **helper-v0.1.12**. Bis dieser veröffentlicht ist, findest du dort nur die älteren Pakete. „Source code“ und „Code → Download ZIP“ sind keine Installationspakete.

| Verwendung | Datei |
| --- | --- |
| Installation mit Helper; beide Mod-Ausgaben enthalten | **Extended-Hotbar-Helper-0.1.12-test.zip** |
| Manuelle Installation für MelonLoader | **Extended-Hotbar-0.3.8.zip** |
| Manuelle Installation für BepInEx | **Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip** |
| Zusätzlich für ein lokales Update aus einem älteren Helper | **Extended-Hotbar-Helper-0.1.12-test.zip.update.json** |
| Optionale Prüfsummen | **SHA256SUMS.txt** |

Wähle die Helper-ZIP oder die zu deinem Loader passende Mod-ZIP. Nur das MelonLoader-Mod-Paket enthält den optionalen Demo-Spielstand DS_B.0.4.17. Der Helper kann das ausgewählte eingebettete Paket exportieren. Kein Spielstand wird automatisch importiert.

## Voraussetzungen und Start

- Windows x64; Dungeon Settlers **DS_B.0.4.19 / Steam-Build 25154317**.
- Separat installiertes **MelonLoader 0.7.3** oder **BepInEx 6 Unity IL2CPP x64**, getestet mit **6.0.0-be.788+5b766a3**. BepInEx 5 und Mono werden nicht unterstützt. Loader nicht in derselben Spielkopie mischen.
- Der Helper benötigt **.NET Framework 4.8**. Spiel und Loader werden nicht mitgeliefert oder vom Helper installiert.

1. Spielstände sichern und alle Spielinstanzen schließen. Spielkopien teilen normalerweise dieselben Saves und Einstellungen; eine Kopie ist kein Save-Backup.
2. Die ganze Helper-ZIP in einen eigenen Ordner entpacken und `Extended-Hotbar-Helper.exe` öffnen.
3. Richtigen Spielordner und Loader prüfen, dann die Mod-Installation bestätigen. Fehlende, falsche oder gemischte Loader werden abgewiesen.
4. Danach das Spiel selbst starten. Andere Mods bleiben unangetastet.

Manuell: Beim MelonLoader-Paket den enthaltenen `Mods`-Ordner mit dem Spielordner zusammenführen; beim BepInEx-Paket den `BepInEx`-Ordner. DLL und mitgelieferter Grafikordner gehören zusammen. Vor einem Update alte Hotbar-Dateien außerhalb des Mod-Ordners sichern. Details in den Paket-Anleitungen.

## Tasten selbst wählen

Die Installation verändert keine Belegungen. Native Standardtasten bleiben erhalten; zusätzliche Skill- und Item-Aktionen starten in beiden Spalten unbelegt. Bereits gespeicherte Belegungen und alte Konflikte werden nicht ungefragt geändert.

Belege die Tasten im Spiel selbst oder wende ausdrücklich das optionale Helper-Profil an: Skills **1–0**, Charaktere **linke Umschalttaste + 1–0**, Items **Q/E/R**, additive Auswahl unbelegt. Das Profil ersetzt beide Spalten dieser Aktionen. Weitere belegte Aktionen werden nur nach zusätzlicher Konfliktbestätigung geändert. Vorschau, Sicherung und Rücknahme sind vorhanden; reine Charakterprofile bleiben verfügbar.

## Lokale Updates

Downloads erfolgen manuell; Online-Updates bleiben deaktiviert. Für ein Update aus einem älteren Helper müssen neue Helper-ZIP und passende signierte `.zip.update.json` gemeinsam im Windows-Downloadordner liegen. „Nur Helper aktualisieren“ installiert keine Mod. Neue EXE: `%LOCALAPPDATA%\ExtendedHotbarHelper\Updates`; alte EXE und Verknüpfungen bleiben erhalten. Für BepInEx ist Helper 0.1.12 nötig.

Prüfsummen ersetzen keine Signatur. Die Paketsignatur ist keine Windows-Authenticode-Signatur; eine Windows-Startwarnung kann weiterhin erscheinen.

## Prüfstand und Grenzen

Die Entwicklung meldet 224 erfolgreiche Helper-Tests, 33 Mod-Tests mit dem bereitgestellten Save-Testfall, beide Loader-Builds ohne Warnungen/Fehler und 150 UI-Vorschauen in zehn Sprachen. Danny bestätigte am 10.09.2026 auch das Einstellen der Tasten an der Steam-Version und weitere Funktionen als erfolgreich. Das ist keine vollständige Langzeitkampagnen- oder Fremdmod-Garantie. Die unveränderten ZIP-Anleitungen dokumentieren teilweise noch den früheren, konservativeren Teststand.

Die BepInEx-Meldung `Class::Init signatures have been exhausted, using a substitute!` bleibt sichtbar; die dokumentierten Tests bestehen trotzdem. Unbekannte Spielbuilds bleiben blockiert.

Die experimentelle Rückkehr zu Vanilla erzeugt einen zusätzlichen manuellen Save **in derselben Kampagne** und entfernt Extended Hotbar. Das Original wird bewahrt und gesichert; Autosaves sind gemeinsam. Andere Mods und der Loader bleiben installiert.

[Helper-Anleitung](HotbarHelper/BITTE%20ZUERST%20LESEN.txt) · [MelonLoader-Anleitung](DungeonSettlers10Slots/UserGuide/BITTE%20ZUERST%20LESEN.txt) · [BepInEx-Anleitung](DungeonSettlersHotbar.BepInEx/README.md) · [Selbst bauen](BUILDING.md)

Inoffizielles Community-Projekt, kein offizielles Produkt von CanOpener oder dem Publisher. Eigener Code und eigene Anleitungen: MIT; Spielgrafik, Demo und Spielinhalte sind ausgeschlossen. Siehe [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) und [Grafikhinweis](Assets/NOTICE.txt).
