# Extended Hotbar: BepInEx 1.0.2

Zehn aktive Skill-Slots, separater Basisangriff und drei Item-Slots für
Dungeon Settlers. Eigener BepInEx-Einstieg, gemeinsame Spiellogik mit der
MelonLoader-Version 1.0.2. Version 1.0 kennzeichnet den funktionsvollständigen,
stabilisierten Umfang; die unten genannten Testgrenzen bleiben bestehen.

## Voraussetzungen

- Windows x64; Steam-Build 25846389 (DS_B.0.4.29).
- Separates Spielverzeichnis mit BepInEx **Unity.IL2CPP win-x64**, getestet mit
  **6.0.0-be.788+5b766a3**. BepInEx 5 / Mono ist hierfür falsch.
- Steam muss mit einer berechtigten Spielinstallation verfügbar sein.
- MelonLoader und BepInEx nicht in dieselbe Spielkopie mischen.

Loader separat beziehen: https://builds.bepinex.dev/projects/bepinex_be

Offizielle Anleitung:
https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html

## Installation und Start

1. Alle Spielinstanzen schließen und Spielstände sichern.
2. BepInEx in einer separaten Kopie des unterstützten Spiels installieren und
   einmal starten, damit `BepInEx/interop` erzeugt wird. Spiel wieder schließen.
3. Den Ordner `BepInEx` aus diesem Plugin-Paket mit dem Spielordner zusammenführen.
   Die DLL gehört nach `BepInEx/plugins/ExtendedHotbar/`; die Grafik bleibt im
   direkt danebenliegenden Ordner `DungeonSettlers10SlotsAssets`.
4. Diese Spielkopie starten und zunächst eine neue Testkampagne verwenden.
   In `BepInEx/LogOutput.log` muss die BepInEx-Hotbar ohne Ladefehler erscheinen.

Der erste Loader-Start kann länger dauern und Unity-Schnittstellen nachladen.
Die Installation ändert keine Tasten. Zusätzliche Skill- und Item-Plätze bleiben
standardmäßig unbelegt. Belege sie selbst in den Spieloptionen. Kein Zahlen-,
Q/E/R- oder Shift-Profil wird automatisch angewendet. Bestehende Belegungen bleiben erhalten;
alte Kollisionen werden nicht ungefragt aus gespeicherten Einstellungen gelöscht.

Wichtig: Eine Spielkopie bedeutet KEINE getrennten Spielstände. Beide Loader
verwenden weiterhin `%USERPROFILE%/AppData/LocalLow/CanOpener/Dungeon Settlers`.
Kampagnen-Autosaves sind gemeinsam; nie beide Spielkopien gleichzeitig öffnen.
Keine Spielstände werden durch dieses Plugin-Paket importiert. Bestehende
Tastenbelegungen werden beim normalen Start nicht automatisch überschrieben.

Falls Steam beim direkten Start zurück zur Originalinstallation springt:
für die lokale Entwicklungskopie eine `steam_appid.txt` mit `2798330` neben
der EXE verwenden und das Arbeitsverzeichnis auf diese Kopie setzen. Steam
und die normale Spielberechtigung bleiben erforderlich.
Diese Datei gehört nicht zum Plugin-Paket.
Siehe https://partner.steamgames.com/doc/api/steam_api#SteamAPI_RestartAppIfNecessary

## Entfernen und Zurückwechseln

Spiel schließen. Den Unterordner `BepInEx/plugins/ExtendedHotbar` aus dieser
Kopie herausnehmen. Kein Entfernen im laufenden Spiel. Die normale Steam-Kopie
mit MelonLoader ist davon unabhängig. Mod-Spielstände bleiben Mod-Spielstände:
das Entfernen des Plugins ist keine Spielstand-Konvertierung zu Vanilla.

## Prüfstand und Grenzen

Am 10.10.2026 auf Steam-Build 25846389 (DS_B.0.4.29) geprüft, unter BepInEx
und unter MelonLoader: beide Loader-Builds, reine Datentests, native zehn Slots,
GUID-Trennung, Save/Load inklusive tatsächlichem temporärem Dateischreiben,
Erhalt alter 12-Slot-Daten, drei Item-Slots, Tastaturfilter und Belegungen,
Tastennamen nach Tastaturlayout, zehn Sprachen, Rahmen-Lebensdauer sowie
BepInEx-Update-/Szenen-/Quit-Callbacks. Zusätzlich: Skill- und Item-Kontextmenüs
mit None, Shift, Ctrl und Alt. Unter BepInEx wurde außerdem eine Kampagne geladen
und die Hotbar mit ihren Tastennamen geprüft. BepInEx beendet den Test sauber.
In beiden Loadern wurden Kampagnen gemeinsam mit Dungeon Settlers Delvers: Core 0.4.1
und Frieren 0.4.1 geladen; beide erkennen die Hotbar 1.0.2 als kompatibel.
Aktuelle Details: `../DungeonSettlers10Slots/PATCH-25846389.md` im Quellbaum.
`VALIDATION.md` enthält historische Tests auf 0.4.19.

1.0.2 passt die Mod an das Spielupdate DS_B.0.4.29 an. Die Tastennamen folgen dem
Tastaturlayout aus den Spieloptionen. Außerdem enthält die DLL keinen lokalen
Build-Pfad mehr, und Autoren- und Lizenzangaben sind vereinheitlicht.

1.0.1 (26.09.2026) ist eine Wartungsausgabe ohne neuen Funktionsumfang. Bei
unbekanntem Spielbuild deaktiviert sich das Plugin jetzt nur mit einer Warnung
und meldet keinen Ladefehler mehr. Das Log enthält keinen lokalen Spielordner
mehr. Speicherpfad-Abgleich und Item-Eingabe sind robuster, und die
Rahmenprüfung im UI-Prüfmodus ist korrigiert.

Die nativen Prüfläufe einschließlich UI-Prüfungen bestanden mit geladener
Kampagne, gemeinsam mit Dungeon Settlers Delvers: Core 0.2.0.
Details: `../DungeonSettlers10Slots/CHANGELOG.md`.

Noch offen: tatsächliche Tastendrücke und sichtbare Hotbar im Kampf, längere
Kampagnen und Zusammenspiel mit weiteren BepInEx-Mods. Die neue Kopie enthält
nur diese Hotbar, nicht automatisch die übrigen MelonLoader-Addons.

Der getestete Loader meldet `Class::Init signatures have been exhausted,
using a substitute!`. Die genannten Tests bestehen trotzdem. Diese Loader-
Warnung wird nicht unterdrückt. Unbekannte Spielcode-/Metadaten-Kombinationen
deaktivieren die Hotbar. Zusätzlich prüft sie vor dem Patchen den nativen
Wörterbuchzugriff für Speicher-Datensätze.

## Quellcode bauen

Benötigt .NET SDK mit .NET-6-Zielunterstützung und eine bereits initialisierte
BepInEx-Spielkopie. Keine Spiel-DLLs in GitHub hochladen. MelonLoader-generierte
Schnittstellen sind kein Ersatz für die BepInEx-Schnittstellen.

```powershell
$gameDir = 'C:\Pfad\zur\aktualisierten\BepInEx-Spielkopie'
dotnet build .\DungeonSettlersHotbar.BepInEx\DungeonSettlersHotbar.BepInEx.csproj -c Release "-p:GameDir=$gameDir"
pwsh -File .\DungeonSettlersHotbar.BepInEx\Build-Package.ps1 -GameDir $gameDir
```

Das Paket enthält nur Plugin, Rahmen und Hinweise; kein Spiel, keinen Loader,
keine generierten DLLs und keinen Testspielstand. Keine GitHub-Aktion erfolgt
durch das Build-Skript. `DungeonSettlers10Slots` ist als gemeinsamer Quellordner
weiterhin erforderlich; dort verbleibt auch der MelonLoader-Einstieg.

## English summary

Feature-complete BepInEx 6 Unity IL2CPP x64 port tested with Steam build 25846389
(DS_B.0.4.29).
Install the package's `BepInEx` folder into a separate BepInEx game copy, with
the plugin DLL and its adjacent assets kept together. Never mix loaders.
Extra bindings start empty; existing bindings are preserved. Choose keys in
the game options; no number-key, Q/E/R or Shift preset is applied. Both copies share
the real save/settings directory: back up first, close all copies, and begin
with a new test campaign. Native tests on 0.4.29 passed for slots, persistence,
bindings, key names per keyboard layout, localization, frame lifetime, loader callbacks
and None/Shift/Ctrl/Alt context-menu routing; a loaded campaign passed the key-name check.
Under both loaders, campaigns were loaded alongside Delvers Core 0.4.1 and Frieren 0.4.1;
both recognize Hotbar 1.0.2 as compatible.
1.0.2 adapts the mod to the DS_B.0.4.29 game update: key names follow the keyboard
layout chosen in the game options, the DLL no longer contains a local build path,
and author and license notes are unified.
1.0.1 is a maintenance release. On an unknown game build it now disables itself
with a warning instead of a load error, and its log no longer contains the local
game folder. It also makes save-path matching more robust and fixes the
UI-audit frame check. Its native checks, including the UI
checks, passed with a loaded campaign alongside Delvers Core 0.2.0. Real combat, visible HUD,
long sessions and third-party compatibility still require playtesting.
Original mod code is MIT; the edited game artwork is not. See the notices.
