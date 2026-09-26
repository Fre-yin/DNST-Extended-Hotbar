# Extended Hotbar

**Deutsch** | [English](README.en.md)

Zehn aktive Fähigkeitsplätze, ein separater Basisangriff und drei Gegenstandsplätze für Dungeon Settlers.

**Version 1.0.1 für MelonLoader und BepInEx — Wartungsausgabe der funktionsvollständigen 1.0.**

## Downloads

Lade das eigenständige Mod-Paket für deinen installierten Loader herunter:

| Loader | Mod-Paket |
| --- | --- |
| MelonLoader | [Extended-Hotbar-1.0.1.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v1.0.1/Extended-Hotbar-1.0.1.zip) |
| BepInEx | [Extended-Hotbar-BepInEx-1.0.1.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v1.0.1/Extended-Hotbar-BepInEx-1.0.1.zip) |

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

1.0.1 ist eine Wartungsausgabe ohne neuen Funktionsumfang. Spielstandformat, Tastenbelegungen, Layout und Grafik sind gegenüber 1.0.0 unverändert. Die Änderungen:
- BepInEx deaktiviert sich bei unbekanntem Spielbuild jetzt wie MelonLoader nur mit einer Warnung.
- Das BepInEx-Log enthält keinen lokalen Spielordner mehr.
- Der Speicherpfad-Abgleich ist robuster, und ein unbestätigtes Speichern wird im Log gemeldet.
- Die Warteschlange der Item-Eingabe wird sauberer bereinigt.
- Eine fehlerhafte Rahmenprüfung im UI-Prüfmodus ist korrigiert.

Einzelheiten stehen im [Changelog](DungeonSettlers10Slots/CHANGELOG.md).

Beide 1.0.1-Builds liefen mit 0 Warnungen und 0 Fehlern. Die Pakettests bestanden mit 33/33 für MelonLoader und 32/32 für BepInEx. Die BepInEx-DLL bestand die nativen Prüfläufe einschließlich UI-Prüfungen mit geladener Kampagne, gemeinsam mit Dungeon Settlers Delvers: Core 0.2.0. Die MelonLoader-DLL lud gemeinsam mit Delvers Core und Frieren 0.2.0. Die vollständigen nativen 1.0.0-Prüfungen auf Steam-Build 25284551 sind im [Patchbericht](DungeonSettlers10Slots/PATCH-25284551.md) beschrieben.

Version 1.0 bezeichnet den abgeschlossenen Funktionsumfang. Sichtbarer Kampf- und Langzeitbetrieb, Kampagnenwechsel, alle Auflösungen und beliebige Fremdmod-Kombinationen sind dadurch nicht vollständig geprüft. Die Prüfläufe veränderten keine reale Kampagne, persönlichen Tastenbelegungen oder Steam-Dateien. Abweichende Spielcode- oder Metadaten-Fingerabdrücke werden weiterhin blockiert.

Der getestete BepInEx-Loader meldet `Class::Init signatures have been exhausted, using a substitute!`; die dokumentierten Tests bestehen trotz dieser Meldung.

Das Entfernen der Mod konvertiert Mod-Spielstände nicht automatisch zu Vanilla-Spielständen. Bewahre Sicherungen auf.

## Projektfokus

Die weitere Arbeit konzentriert sich auf die beiden eigenständigen Hotbar-Mods. Für Installation und Tastenbelegung gilt die Anleitung auf dieser Seite.

[Selbst bauen](BUILDING.md)

Inoffizielles Community-Projekt, kein offizielles Produkt von CanOpener oder dem Publisher. Eigener Code und eigene Anleitungen: MIT; Spielgrafik, Demo und Spielinhalte sind ausgeschlossen. Siehe [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) und [Grafikhinweis](Assets/NOTICE.txt).
