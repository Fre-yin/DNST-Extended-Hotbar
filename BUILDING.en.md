# Building Extended Hotbar

[Deutsch](BUILDING.md) | **English** | [Project description](README.en.md)

This guide is for developers. Players should use a ready-to-install release package; they do not need these steps.

## Requirements

- Windows x64 and your own installation of Dungeon Settlers **DS_B.0.4.19 / Steam 25154317**.
- **MelonLoader 0.7.3** in your game folder. Start the game with it once to generate the required interfaces under `MelonLoader/Il2CppAssemblies`, then close the game.
- A .NET SDK capable of targeting `net6.0`, the .NET 6 reference packs, and a .NET 6 runtime for the tests. The local verification environment uses SDK **10.0.302** with .NET 6 support installed. Use `dotnet --list-sdks` and `dotnet --list-runtimes` to check your installed versions.
- **PowerShell 7** (`pwsh`) for the optional packaging script. A terminal is sufficient for the direct DLL build.

Game, Unity and loader DLLs are referenced only from your own installation. They are not included in this repository and must not be uploaded as build dependencies. The build explicitly uses the included `NuGet.Config` with no package sources; no NuGet packages are required. Any missing .NET reference packs must therefore be installed locally before building.

## Build the DLL

Open a terminal in the repository root, where this guide is located. Replace the example path with your actual game folder:

```powershell
dotnet build .\DungeonSettlers10Slots\DungeonSettlers10Slots.csproj -c Release "-p:GameDir=C:\Games\Dungeon Settlers"
```

The result is `DungeonSettlers10Slots/bin/Release/net6.0/DungeonSettlers10Slots.dll`. Nothing is automatically installed into the game. There is no hard-coded game path in the project. A missing `GameDir` or missing local DLLs will stop the build with an explanatory message.

The frame image is not included in this source repository. You can compile the DLL without it, but that does not produce a complete installation package with the custom layout. In the game, the existing code skips the custom frame layout when the image is missing.

## Run the tests

These tests do not require the game, MelonLoader or the frame image:

```powershell
dotnet run --project .\tests\Hotbar.Tests\Hotbar.Tests.csproj -c Release
```

They cover additional-slot data, the save format, bounds and localized labels, among other checks. They do not replace in-game layout and input testing. Additional check paths within the mod code are not a separate QA mod; no QA Toolkit is included.

Optionally, you can pass your own prepared `10SlotsTestfile.json` as an additional test argument. It is not included in the repository and is not required for the normal tests.

## Optional: build an installation ZIP

The complete package also requires your authorized local copy of the edited frame image. Place it at `DungeonSettlers10Slots/Assets/SkillFrame__sharedassets0_mod_4898.png`. It is expressly not MIT-licensed; see [Assets/NOTICE.txt](Assets/NOTICE.txt) and [LICENSING.txt](LICENSING.txt). Git ignores this file.

```powershell
pwsh -File .\DungeonSettlers10Slots\Build-Release.ps1 -GameDir "C:\Games\Dungeon Settlers"
```

The script builds the DLL, runs the tests and creates a verified ZIP under `DungeonSettlers10Slots/dist`. Existing output is never overwritten. `-PackageRevision` identifies the package revision, not a new mod version. Use an unused revision number for another package.

New packages always include the optional demo save. Explicitly place it at `DungeonSettlers10Slots/TestSave/Saves/10SlotsTestfile.json`. The legacy `-IncludeTestSave` switch remains for callers; no separate no-demo variant is created. Git ignores this folder. The script does not scan or modify personal saves. A missing frame image or prepared demo stops packaging.

The exclusions in `.gitignore` apply to Git, not manual browser uploads. Do not upload generated build directories, locally added artwork or save files alongside the source through the GitHub website.

The source is at version **0.3.8** and still checks the supported game build's fingerprints. Do not simply change that check to enable an unknown game update: interfaces and behavior must be verified again first.

## Optional: build Helper 0.1.12

Use a short checkout path, such as `C:\Dev\ExtendedHotbar`. Isolated tests create nested folders; long base paths may hit the Windows/.NET Framework path-length limit. `Build.ps1` also accepts a short `-OutputDirectory`. `Package.ps1` builds internally under `HotbarHelper/bin`, so use a short checkout for packaging.

The helper is a separate Windows application. Building requires a .NET SDK with Roslyn, PowerShell and the **.NET Framework 4.8 Developer Pack**. Running it only requires the .NET Framework 4.8 runtime. Game/loader assemblies and NuGet packages are not needed for this build.

Also download **Extended-Hotbar-0.3.8.zip** from release **helper-v0.1.12**. This standard package includes the optional demo and has SHA256 `D7824BD6B31E3760EBC803111261B75EC5ADF60FAE87B7AAC444DF9B2993F544`. Older ZIPs with the same filename may have different contents. The helper requires the exact hash; repacking is not a replacement. Do not include the ZIP in a source upload.

```powershell
pwsh -File .\HotbarHelper\Build.ps1 -PackagePath "C:\Downloads\Extended-Hotbar-0.3.8.zip" -BepInExPackagePath "C:\Downloads\Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip"
```

Output: `HotbarHelper/bin/Extended-Hotbar-Helper.exe`. The build automatically runs isolated helper tests with the authored `SyntheticSave.json` fixture, which is not a playable campaign save. Personal game/save folders are not modified. Test output is retained under `HotbarHelper/bin/test-runs`.

```powershell
pwsh -File .\HotbarHelper\Package.ps1 -PackagePath "C:\Downloads\Extended-Hotbar-0.3.8.zip" -BepInExPackagePath "C:\Downloads\Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip"
```

This also creates a helper ZIP with instructions in ten languages and licensing notices. Existing packages are never overwritten; select a new `-OutputDirectory` when repeating. Details, optional checks and limitations: [HotbarHelper/README.md](HotbarHelper/README.md).

The user reported a successful in-game save-conversion test. Automated checks still do not establish complete campaign/long-session compatibility. Helper 0.1.12 is prepared as a prerelease; online updates remain disabled.

## BepInEx 0.3.8-bepinex.1 / Helper 0.1.12

Der Helper benötigt beide unveränderten Mod-ZIPs aus helper-v0.1.12. / The Helper requires both unchanged mod ZIPs from helper-v0.1.12.
Zusätzliches BepInEx-Paket / Additional BepInEx package: `Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip`, SHA256 `77193451CD045D03A50E24A80F2D0582E178F335D90926EDF9AAF9E6B3880547`.
Vor Veröffentlichung dieses Releases stehen die neuen Downloadquellen noch nicht bereit. / These download resources become available when that release is published.

Die BepInEx-Ausgabe benötigt eine eigene initialisierte BepInEx 6 Unity IL2CPP x64 Spielkopie (getestet: 6.0.0-be.788+5b766a3), keine MelonLoader-Referenzen. / The BepInEx edition requires its own initialized BepInEx 6 Unity IL2CPP x64 game copy (tested: 6.0.0-be.788+5b766a3), not MelonLoader references.

```powershell
dotnet build .\DungeonSettlersHotbar.BepInEx\DungeonSettlersHotbar.BepInEx.csproj -c Release "-p:GameDir=C:\Games\DungeonSettlers-BepInEx"
pwsh -File .\DungeonSettlersHotbar.BepInEx\Build-Package.ps1 -GameDir "C:\Games\DungeonSettlers-BepInEx"
```

Für das BepInEx-Paket wird dieselbe berechtigt verwendbare lokale Rahmengrafik unter DungeonSettlers10Slots/Assets benötigt, aber kein Demo-Spielstand. / BepInEx packaging requires the same authorized local frame image under DungeonSettlers10Slots/Assets, but no demo save.
Gemeinsamer Quellcode bleibt unter DungeonSettlers10Slots und wird verlinkt. / Shared sources remain in DungeonSettlers10Slots and are linked.

Aktueller Teststand / Current validation: siehe / see [README](README.md#prüfstand-und-grenzen) / [English](README.en.md#validation-and-limitations).
