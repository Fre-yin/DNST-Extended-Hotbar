> Aktueller Stand / Current status (2026-09-10): Helper 0.1.12; MelonLoader 0.3.8; BepInEx 0.3.8-bepinex.1. Danny bestätigte Tasten-Einstellungen an der Steam-Version und weitere Funktionen als erfolgreich. Keine vollständige BepInEx-Kampf-/Langzeitabnahme ableiten. / Danny confirmed Steam key configuration and other functions; this is not a full BepInEx combat or long-session acceptance.

# Extended Hotbar — BepInEx preview 0.3.8-bepinex.1

Zehn aktive Skill-Slots, separater Basisangriff und drei Item-Slots für
Dungeon Settlers. Eigener BepInEx-Einstieg, gemeinsame Spiellogik mit der
MelonLoader-Version 0.3.8. Noch keine als stabil freigegebene Spieltest-Version.

## Voraussetzungen

- Windows x64; Dungeon Settlers DS_B.0.4.19 / Steam-Build 25154317.
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
Hotbar Helper 0.1.12 unterstützt beide Ausgaben. Dort BepInEx auswählen.
Ältere Helper unterstützen dieses Paket nicht. Die Installation ändert keine
Tasten. Zusätzliche Skill- und Item-Plätze bleiben standardmäßig unbelegt.
In den Spieloptionen selbst belegen oder das optionale Profil im Helper mit
Konfliktprüfung ausdrücklich anwenden. Bestehende Belegungen bleiben erhalten;
alte Kollisionen werden nicht ungefragt aus gespeicherten Einstellungen gelöscht.

Wichtig: Eine Spielkopie bedeutet KEINE getrennten Spielstände. Beide Loader
verwenden weiterhin `%USERPROFILE%/AppData/LocalLow/CanOpener/Dungeon Settlers`.
Kampagnen-Autosaves sind gemeinsam; nie beide Spielkopien gleichzeitig öffnen.
Keine Spielstände werden durch dieses Plugin-Paket importiert. Bestehende
Tastenbelegungen werden beim normalen Start nicht automatisch überschrieben.

Falls Steam beim direkten Start zurück zur Originalinstallation springt:
für die lokale Entwicklungskopie eine `steam_appid.txt` mit `2798330` neben
der EXE verwenden und das Arbeitsverzeichnis auf diese Kopie setzen. Steam
und die normale Spielberechtigung bleiben erforderlich. Sie gehört nicht zum Plugin-Paket.
Siehe https://partner.steamgames.com/doc/api/steam_api#SteamAPI_RestartAppIfNecessary

## Entfernen und Zurückwechseln

Spiel schließen. Den Unterordner `BepInEx/plugins/ExtendedHotbar` aus dieser
Kopie herausnehmen. Kein Entfernen im laufenden Spiel. Die normale Steam-Kopie
mit MelonLoader ist davon unabhängig. Mod-Spielstände bleiben Mod-Spielstände:
das Entfernen des Plugins ist keine Spielstand-Konvertierung zu Vanilla.

## Prüfstand und Grenzen

Automatisch geprüft: beide Loader-Builds, reine Datentests, native zehn Slots,
GUID-Trennung, Save/Load inklusive tatsächlichem temporärem Dateischreiben,
Erhalt alter 12-Slot-Daten, drei Item-Slots, Tastaturfilter und Belegungen,
zehn Sprachen, Rahmen-Lebensdauer sowie BepInEx-Update-/Szenen-/Quit-Callbacks.
Details: `VALIDATION.md` im Quellordner.

Nicht vollständig abgenommen: BepInEx-Kampf/HUD, längere Kampagnen und
Zusammenspiel mit anderen BepInEx-Mods. Die zusätzliche Steam-Bedienbestätigung
vom 10.09.2026 ist in VALIDATION.md berücksichtigt. Die neue Kopie enthält
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
dotnet build .\DungeonSettlersHotbar.BepInEx\DungeonSettlersHotbar.BepInEx.csproj -c Release "-p:GameDir=C:\Games\DungeonSettlers-BepInEx"
pwsh -File .\DungeonSettlersHotbar.BepInEx\Build-Package.ps1 -GameDir 'C:\Games\DungeonSettlers-BepInEx'
```

Das Paket enthält nur Plugin, Rahmen und Hinweise; kein Spiel, keinen Loader,
keine generierten DLLs und keinen Testspielstand. Keine GitHub-Aktion erfolgt
durch das Build-Skript. `DungeonSettlers10Slots` ist als gemeinsamer Quellordner
weiterhin erforderlich; dort verbleibt auch der MelonLoader-Einstieg.

## English summary

Experimental BepInEx 6 Unity IL2CPP x64 port for DS_B.0.4.19 / Steam 25154317.
Install the package's `BepInEx` folder into a separate BepInEx game copy, with
the plugin DLL and its adjacent assets kept together. Never mix loaders.
Hotbar Helper 0.1.12 supports this package when BepInEx is selected. Older
helpers do not. Extra bindings start empty; existing bindings are preserved.
Bind keys in game options or explicitly apply the optional Helper profile. Both copies share
the real save/settings directory: back up first, close all copies, and begin
with a new test campaign. Tests pass for native slots, persistence, bindings,
localization, frame lifetime and loader callbacks; real combat, visible HUD,
long sessions and third-party compatibility still require playtesting.
Original mod code is MIT; the edited game artwork is not. See the notices.
