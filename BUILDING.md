# Extended Hotbar selbst bauen

**Deutsch** | [English](BUILDING.en.md) | [Zur Projektbeschreibung](README.md)

Diese Anleitung beschreibt die Builds der eigenständigen MelonLoader- und BepInEx-Mod. Wer die Mod nur spielen möchte, findet die fertigen Mod-Pakete und die Installation in der [Projektbeschreibung](README.md).

## Voraussetzungen für den MelonLoader-Build

- Windows x64 und eine eigene Installation von Dungeon Settlers **DS_B.0.4.23 / Steam 25269660**.
- **MelonLoader 0.7.3** im eigenen Spielordner. Starte das Spiel damit einmal, damit die benötigten Schnittstellen unter `MelonLoader/Il2CppAssemblies` erzeugt werden. Beende das Spiel anschließend.
- Ein .NET SDK, das `net6.0` bauen kann, die .NET-6-Referenzpakete und eine .NET-6-Laufzeit für die Tests. Der lokale Prüfstand verwendet SDK **10.0.302** mit installierter .NET-6-Unterstützung. `dotnet --list-sdks` und `dotnet --list-runtimes` zeigen die vorhandenen Versionen.
- Für das optionale Paketierungsskript: **PowerShell 7** (`pwsh`). Für den direkten DLL-Build reicht ein Terminal.

Spiel-, Unity- und Loader-DLLs werden ausschließlich aus deiner eigenen Installation referenziert. Sie liegen nicht im Repository und dürfen nicht als Build-Abhängigkeiten mit hochgeladen werden. Der Build verwendet ausdrücklich die mitgelieferte `NuGet.Config` ohne Paketquellen; es werden keine NuGet-Pakete benötigt. Fehlende .NET-Referenzpakete müssen deshalb vor dem Build lokal installiert sein.

## DLL bauen

Öffne ein Terminal im Repository-Hauptordner, also dort, wo diese Anleitung liegt. Ersetze den Beispielpfad durch deinen tatsächlichen Spielordner:

```powershell
dotnet build .\DungeonSettlers10Slots\DungeonSettlers10Slots.csproj -c Release "-p:GameDir=C:\Games\Dungeon Settlers"
```

Das Ergebnis liegt unter `DungeonSettlers10Slots/bin/Release/net6.0/DungeonSettlers10Slots.dll`. Es wird nichts automatisch ins Spiel installiert. Der Spielpfad ist nicht fest im Projekt hinterlegt. Ohne `GameDir` oder bei fehlenden lokalen DLLs bricht der Build mit einem Hinweis ab.

Die Rahmengrafik ist im Quellcode-Repository nicht enthalten. Die DLL lässt sich ohne sie kompilieren; das ist aber noch kein vollständiges Installationspaket mit dem angepassten Layout. Im Spiel überspringt der bestehende Code das eigene Rahmenlayout, wenn die Grafik fehlt.

## Tests ausführen

Diese Tests benötigen weder das Spiel noch MelonLoader oder die Rahmengrafik:

```powershell
dotnet run --project .\tests\Hotbar.Tests\Hotbar.Tests.csproj -c Release
```

Sie prüfen unter anderem Zusatzslot-Daten, Speicherformat, Begrenzungen und Sprachbezeichnungen. Sie ersetzen keinen Spieltest für Darstellung und Eingabe. Die zusätzlichen Prüfpfade im Modcode sind keine separate QA-Mod; es wird kein QA-Toolkit mitgeliefert.

Optional kann ein eigener, vorbereiteter `10SlotsTestfile.json` als zusätzliches Testargument angegeben werden. Die Datei ist nicht im Repository enthalten; für die normalen Tests ist sie nicht erforderlich.

## Optional: ein Installations-ZIP bauen

Für das vollständige Paket brauchst du zusätzlich deine berechtigt verwendbare lokale Kopie der bearbeiteten Rahmengrafik. Lege sie unter `DungeonSettlers10Slots/Assets/SkillFrame__sharedassets0_mod_4898.png` ab. Sie ist ausdrücklich nicht MIT-lizenziert; siehe [Assets/NOTICE.txt](Assets/NOTICE.txt) und [LICENSING.txt](LICENSING.txt). Die Datei wird von Git ignoriert.

```powershell
pwsh -File .\DungeonSettlers10Slots\Build-Release.ps1 -GameDir "C:\Games\Dungeon Settlers"
```

Das Skript baut die DLL, führt die Tests aus und erstellt ein geprüftes ZIP unter `DungeonSettlers10Slots/dist`. Bereits vorhandene Ausgaben werden nicht überschrieben. Die Nummer nach `-PackageRevision` bezeichnet nur die Paketausgabe, nicht eine neue Mod-Version. Wähle bei einem weiteren Paket eine noch unbenutzte Nummer.

Neue Pakete enthalten den optionalen Demo-Spielstand immer. Lege ihn gezielt unter `DungeonSettlers10Slots/TestSave/Saves/10SlotsTestfile.json` ab. Der alte Schalter `-IncludeTestSave` bleibt für Aufrufer erhalten; eine Variante ohne Demo wird nicht mehr erstellt. Dieser Ordner wird von Git ignoriert. Das Skript durchsucht oder verändert keine persönlichen Spielstandordner. Ohne die Grafik oder den vorbereiteten Demo-Spielstand bricht es vor dem Paketbau ab.

Die Ausschlüsse in `.gitignore` gelten für Git, nicht für manuelle Browser-Uploads. Lade erzeugte Build-Ordner, lokal ergänzte Grafiken und Spielstände daher nicht zusammen mit dem Quellcode über die GitHub-Webseite hoch.

Der Quellcode steht bei Version **0.3.9** und prüft weiterhin die Fingerabdrücke des unterstützten Spielbuilds. Ändere diese Prüfung nicht einfach, um ein unbekanntes Spielupdate freizuschalten; dafür müssen die Schnittstellen und das Verhalten erneut geprüft werden.

## BepInEx 0.3.9-bepinex.1

Die BepInEx-Ausgabe ist ein eigenständiges Mod-Paket. Ihr Build benötigt eine eigene initialisierte BepInEx 6 Unity IL2CPP x64 Spielkopie (getestet: 6.0.0-be.788+5b766a3), keine MelonLoader-Referenzen.

```powershell
dotnet build .\DungeonSettlersHotbar.BepInEx\DungeonSettlersHotbar.BepInEx.csproj -c Release "-p:GameDir=C:\Games\DungeonSettlers-BepInEx"
pwsh -File .\DungeonSettlersHotbar.BepInEx\Build-Package.ps1 -GameDir "C:\Games\DungeonSettlers-BepInEx"
```

Für das BepInEx-Paket wird dieselbe berechtigt verwendbare lokale Rahmengrafik unter `DungeonSettlers10Slots/Assets` benötigt, aber kein Demo-Spielstand. Gemeinsamer Quellcode bleibt unter `DungeonSettlers10Slots` und wird verlinkt.

Aktueller Teststand: siehe [README](README.md#prüfstand-und-grenzen).
