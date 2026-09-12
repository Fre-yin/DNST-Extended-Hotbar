# Extended Hotbar

**Deutsch** | [English](README.en.md)

Zehn aktive Fähigkeitsplätze, ein separater Basisangriff und drei Gegenstandsplätze für Dungeon Settlers.

**Vorabversionen: MelonLoader 0.3.9 · BepInEx 0.3.9-bepinex.1**

## Downloads

Lade das eigenständige Mod-Paket für deinen installierten Loader herunter:

| Loader | Mod-Paket |
| --- | --- |
| MelonLoader | [Extended-Hotbar-0.3.9.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v0.3.9/Extended-Hotbar-0.3.9.zip) |
| BepInEx | [Extended-Hotbar-BepInEx-0.3.9-bepinex.1.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v0.3.9/Extended-Hotbar-BepInEx-0.3.9-bepinex.1.zip) |

Diese Links führen direkt zu den jeweiligen Mod-ZIPs. Die automatisch erzeugten Quellcode-Archive sind keine Installationspakete.

Nur das MelonLoader-Paket enthält den optionalen Demo-Spielstand DS_B.0.4.17. Er wird nicht automatisch importiert und ist für die Mod nicht erforderlich.

## Voraussetzungen und Installation

- Windows x64 und Dungeon Settlers **DS_B.0.4.23 / Steam-Build 25269660**.
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

Beide Mod-Ausgaben bleiben Vorabversionen für den oben genannten Spielbuild. Beide endgültigen Release-Builds liefen mit 0 Warnungen und 0 Fehlern. Dokumentiert sind 33 Datentests mit der unveränderten Demo und 32 ohne Demo sowie native Prüfungen beider Loader für Slots, Kontextmenüs, Tastenbelegungen, temporäres Speichern/Laden, zehn Sprachen und die Lebensdauer des Rahmens.

Ein anschließender sichtbarer BepInEx-Nutzercheck wurde positiv bestätigt. Die einzelnen Kampf-, Klick- und Speicheraktionen wurden dabei nicht protokolliert. Vollständige Kampf-, Kampagnen-, Langzeit- und Fremdmod-Kompatibilität ist daher nicht bestätigt. Unbekannte Spielbuilds werden weiterhin blockiert.

Der getestete BepInEx-Loader meldet `Class::Init signatures have been exhausted, using a substitute!`; die dokumentierten Tests bestehen trotz dieser Meldung.

Das Entfernen der Mod konvertiert Mod-Spielstände nicht automatisch zu Vanilla-Spielständen. Bewahre Sicherungen auf.

## Projektfokus

Die weitere Arbeit konzentriert sich auf die beiden eigenständigen Hotbar-Mods. Für Installation und Tastenbelegung gilt die Anleitung auf dieser Seite.

[Selbst bauen](BUILDING.md)

Inoffizielles Community-Projekt, kein offizielles Produkt von CanOpener oder dem Publisher. Eigener Code und eigene Anleitungen: MIT; Spielgrafik, Demo und Spielinhalte sind ausgeschlossen. Siehe [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) und [Grafikhinweis](Assets/NOTICE.txt).
