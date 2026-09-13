# Extended Hotbar

**Deutsch** | [English](README.en.md)

Zehn aktive Fähigkeitsplätze, ein separater Basisangriff und drei Gegenstandsplätze für Dungeon Settlers.

**Version 1.0.0 für MelonLoader und BepInEx — funktionsvollständige Veröffentlichung.**

## Downloads

Lade das eigenständige Mod-Paket für deinen installierten Loader herunter:

| Loader | Mod-Paket |
| --- | --- |
| MelonLoader | [Extended-Hotbar-1.0.0.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v1.0.0/Extended-Hotbar-1.0.0.zip) |
| BepInEx | [Extended-Hotbar-BepInEx-1.0.0.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v1.0.0/Extended-Hotbar-BepInEx-1.0.0.zip) |

Diese Links führen direkt zu den jeweiligen Mod-ZIPs. Die automatisch erzeugten Quellcode-Archive sind keine Installationspakete.

Nur das MelonLoader-Paket enthält den optionalen Demo-Spielstand DS_B.0.4.17. Er wird nicht automatisch importiert und ist für die Mod nicht erforderlich.

## Voraussetzungen und Installation

- Windows x64 und Dungeon Settlers **Steam-Build 25284551 auf der unveränderten DS_B.0.4.23-Codebasis**. Build 25269660 verwendet dieselben geprüften Spielcode- und Metadaten-Fingerabdrücke.
- Separat installiertes **MelonLoader 0.7.3** oder **BepInEx 6 Unity IL2CPP x64**, getestet mit **6.0.0-be.788+5b766a3**. BepInEx 5 und Mono werden nicht unterstützt.
- Pro Spielordner nur einen Loader verwenden. Die Pakete enthalten weder das Spiel noch den Loader.

1. Spielstände sichern und alle Spielinstanzen schließen. Verschiedene Spielkopien können dieselben Spielstände und Einstellungen verwenden.
2. Das zu deinem Loader passende Mod-ZIP entpacken.
3. Bei MelonLoader den enthaltenen **Mods**-Ordner mit dem Spielordner zusammenführen. Bei BepInEx den enthaltenen **BepInEx**-Ordner mit dem Spielordner zusammenführen.
4. Die mitgelieferte Ordnerstruktur beibehalten: Mod-DLL und Grafikordner gehören zusammen. Bei einem Update die bisherigen Hotbar-Dateien vorher außerhalb des Mod-Ordners sichern.
5. Das Spiel starten, zunächst in einer Testkampagne prüfen und die gewünschten Tasten in den Spieloptionen belegen.

## Tasten selbst wählen

Native Standardtasten und bereits gespeicherte Belegungen bleiben erhalten. Zusätzliche Skill- und Gegenstandsaktionen starten ohne gespeicherte Belegung in beiden Spalten unbelegt.

Wähle deine Tasten selbst in den Spieloptionen. Es gibt kein vorgeschriebenes Tastenprofil. Bereits vorhandene Belegungskonflikte werden nicht automatisch bereinigt; passe bei Bedarf die betroffenen Aktionen einzeln an.

## Prüfstand und Grenzen

Beide endgültigen 1.0.0-Builds liefen mit 0 Warnungen und 0 Fehlern. Die Pakettests bestanden mit 33/33 für MelonLoader und 32/32 für BepInEx. Beide finalen DLLs bestanden auf Steam-Build 25284551 die vollständigen nativen Prüfungen für Slots, Kontextmenüs, Tastenbelegungen, temporäres Speichern/Laden, zehn Sprachen und die Lebensdauer des Rahmens. BepInEx beendete mit Exitcode 0 und sauberem Runtime-Cleanup; MelonLoader wurde nach den bestandenen Prüfungen im vorgesehenen 45-Sekunden-Fenster gezielt beendet.

Version 1.0.0 bezeichnet den abgeschlossenen Funktionsumfang. Sichtbarer Kampf- und Langzeitbetrieb, Kampagnenwechsel, alle Auflösungen und beliebige Fremdmod-Kombinationen sind dadurch nicht vollständig geprüft. Die Prüfläufe veränderten keine reale Kampagne, persönlichen Tastenbelegungen oder Steam-Dateien. Abweichende Spielcode- oder Metadaten-Fingerabdrücke werden weiterhin blockiert.

Gegenüber 0.3.9 blieb die Hotbar-, Save-, Binding-, Eingabe-, Layout- und Asset-Logik unverändert. Der Spielpatch änderte ausschließlich Addressables einschließlich UI-Prefabs und Tabellen. Aktualisiert wurden Versionsangaben, Kompatibilitätsmeldung und Dokumentation. Einzelheiten: [Changelog](DungeonSettlers10Slots/CHANGELOG.md) und [Patchbericht](DungeonSettlers10Slots/PATCH-25284551.md).

Der getestete BepInEx-Loader meldet `Class::Init signatures have been exhausted, using a substitute!`; die dokumentierten Tests bestehen trotz dieser Meldung.

Das Entfernen der Mod konvertiert Mod-Spielstände nicht automatisch zu Vanilla-Spielständen. Bewahre Sicherungen auf.

## Projektfokus

Die weitere Arbeit konzentriert sich auf die beiden eigenständigen Hotbar-Mods. Für Installation und Tastenbelegung gilt die Anleitung auf dieser Seite.

[Selbst bauen](BUILDING.md)

Inoffizielles Community-Projekt, kein offizielles Produkt von CanOpener oder dem Publisher. Eigener Code und eigene Anleitungen: MIT; Spielgrafik, Demo und Spielinhalte sind ausgeschlossen. Siehe [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) und [Grafikhinweis](Assets/NOTICE.txt).
