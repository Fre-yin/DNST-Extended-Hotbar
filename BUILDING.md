# Extended Hotbar selbst bauen

**Deutsch** | [English](BUILDING.en.md) | [Zur Projektbeschreibung](README.md)

Diese Anleitung ist für Entwickler. Wer die Mod nur spielen möchte, braucht das fertige Release-Paket, nicht diese Schritte.

## Voraussetzungen

- Windows x64 und eine eigene Installation von Dungeon Settlers **DS_B.0.4.19 / Steam 25154317**.
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

Mit `-IncludeTestSave` lässt sich der vorbereitete Demo-Spielstand beilegen. Dafür muss er gezielt unter `DungeonSettlers10Slots/TestSave/Saves/10SlotsTestfile.json` liegen. Dieser Ordner wird ebenfalls von Git ignoriert. Das Skript durchsucht oder verändert keine persönlichen Spielstandordner. Ohne die Grafik beziehungsweise ohne den ausdrücklich angeforderten Demo-Spielstand bricht es vor dem Paketbau ab.

Die Ausschlüsse in `.gitignore` gelten für Git, nicht für manuelle Browser-Uploads. Lade erzeugte Build-Ordner, lokal ergänzte Grafiken und Spielstände daher nicht zusammen mit dem Quellcode über die GitHub-Webseite hoch.

Der Quellcode steht bei Version **0.3.7** und prüft weiterhin die Fingerabdrücke des unterstützten Spielbuilds. Ändere diese Prüfung nicht einfach, um ein unbekanntes Spielupdate freizuschalten; dafür müssen die Schnittstellen und das Verhalten erneut geprüft werden.

## Optional: Helfer 0.1.2 bauen

Verwende einen kurzen Projektpfad, beispielsweise `C:\Dev\ExtendedHotbar`. Die isolierten Tests erzeugen verschachtelte Ordner; sehr lange Ausgangspfade können an der Windows/.NET-Framework-Pfadlängengrenze scheitern. Für `Build.ps1` lässt sich auch ein kurzer `-OutputDirectory` angeben. `Package.ps1` baut intern unter `HotbarHelper/bin`; dafür den gesamten Quellcode kurz ablegen.

Der Helfer ist ein separates Windows-Programm. Zum Bauen brauchst du ein .NET SDK mit Roslyn, PowerShell und das **.NET Framework 4.8 Developer Pack**. Zum Ausführen genügt die .NET-Framework-4.8-Laufzeit. Spiel- oder Loader-DLLs und NuGet-Pakete werden für diesen Build nicht benötigt.

Lade zusätzlich das unveränderte **Extended-Hotbar-0.3.7.zip** aus demselben Release herunter. Der Helfer bettet dieses Paket ein und prüft seinen exakten SHA256-Wert; ein selbst neu gepacktes ZIP ist kein gleichwertiger Ersatz. Die ZIP-Datei gehört nicht in den Quellcode-Upload.

```powershell
pwsh -File .\HotbarHelper\Build.ps1 -PackagePath "C:\Downloads\Extended-Hotbar-0.3.7.zip"
```

Ergebnis: `HotbarHelper/bin/Extended-Hotbar-Helper.exe`. Der Build führt automatisch die isolierten Helfertests mit der selbst verfassten `SyntheticSave.json` aus. Das ist kein spielbarer Kampagnenspielstand. Persönliche Spiel-/Speicherordner werden nicht verändert. Testausgaben bleiben unter `HotbarHelper/bin/test-runs` erhalten.

```powershell
pwsh -File .\HotbarHelper\Package.ps1 -PackagePath "C:\Downloads\Extended-Hotbar-0.3.7.zip"
```

Das erstellt zusätzlich ein Helfer-ZIP mit Anleitungen in zehn Sprachen und Lizenzhinweisen. Vorhandene Pakete werden nicht überschrieben; bei Wiederholung einen neuen `-OutputDirectory` angeben. Einzelheiten, optionale Prüfungen und Grenzen: [HotbarHelper/README.md](HotbarHelper/README.md).

Auch bestandene Helfertests sind keine Freigabe der experimentellen Spielstandsumwandlung. Ihr vollständiger Kampagnen-Laden/Speichern/Neuladen-Test steht aus.
