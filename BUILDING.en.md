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

## Optional: build Helper 0.1.13

Use a short checkout path, such as `C:\Dev\ExtendedHotbar`. Isolated tests create nested folders; long base paths may hit the Windows/.NET Framework path-length limit. `Build.ps1` also accepts a short `-OutputDirectory`. `Package.ps1` builds internally under `HotbarHelper/bin-0.1.13`, so use a short checkout for packaging. If `PathTooLongException` occurs, shorten the paths and rerun the same tests.

The helper is a separate Windows application. Building requires a .NET SDK with Roslyn, PowerShell and the **.NET Framework 4.8 Developer Pack**. Running it only requires the .NET Framework 4.8 runtime. Game/loader assemblies and NuGet packages are not needed for this build.

Also download both unchanged mod packages from [Releases](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases). They were already used with **helper-v0.1.12** and are also required for Helper 0.1.13:

| File | SHA256 |
| --- | --- |
| `Extended-Hotbar-0.3.8.zip` | `D7824BD6B31E3760EBC803111261B75EC5ADF60FAE87B7AAC444DF9B2993F544` |
| `Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip` | `77193451CD045D03A50E24A80F2D0582E178F335D90926EDF9AAF9E6B3880547` |

Only the MelonLoader package includes the optional demo save. Older ZIPs with the same filename may have different contents. The Helper requires the exact hashes; repacking is not a replacement. Do not include the ZIPs in a source upload.

```powershell
pwsh -File .\HotbarHelper\Build.ps1 -PackagePath "C:\Downloads\Extended-Hotbar-0.3.8.zip" -BepInExPackagePath "C:\Downloads\Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip"
```

Output: `HotbarHelper/bin/Extended-Hotbar-Helper.exe`. The build automatically runs isolated helper tests with the authored `SyntheticSave.json` fixture, which is not a playable campaign save. Personal game/save folders are not modified. Test output is retained under `HotbarHelper/bin/test-runs`.

```powershell
pwsh -File .\HotbarHelper\Package.ps1 -PackagePath "C:\Downloads\Extended-Hotbar-0.3.8.zip" -BepInExPackagePath "C:\Downloads\Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip"
```

This also creates a helper ZIP with instructions in ten languages and licensing notices. Existing packages are never overwritten; select a new `-OutputDirectory` when repeating. Details, optional checks and limitations: [HotbarHelper/README.md](HotbarHelper/README.md).

By default, packaging does not create a signed `.zip.update.json` for local self-updates. `Package.ps1` provides an explicit `-SignUpdatePackage` option for this; the command above does not enable it. No such file exists for the prepared 0.1.13 package yet; manual distribution by extracting the entire package is supported. Self-update from an older Helper must only be offered with a valid signature file matching that exact package. The normal build uses the existing public `UpdateTrust.xml`, not a private publishing key.

Helper 0.1.13 is prepared as a prerelease. The HTTP client, release feed and online update functions have been removed; optional scanning for existing local packages and their signature checks remain. The complete source list in `Build.ps1` includes `LocalUpdatePreferences.cs` for the program and `LocalOnlyChecks.cs` for tests only.

Earlier reported in-game tests apply to the previous Helper version. No new in-game test was performed for 0.1.13. Automated checks do not establish complete campaign/long-session compatibility; see the documented validation in the [project description](README.en.md#validation-and-limitations).

## BepInEx 0.3.8-bepinex.1

The BepInEx edition remains unchanged for Helper 0.1.13. Its build requires its own initialized BepInEx 6 Unity IL2CPP x64 game copy (tested: 6.0.0-be.788+5b766a3), not MelonLoader references.

```powershell
dotnet build .\DungeonSettlersHotbar.BepInEx\DungeonSettlersHotbar.BepInEx.csproj -c Release "-p:GameDir=C:\Games\DungeonSettlers-BepInEx"
pwsh -File .\DungeonSettlersHotbar.BepInEx\Build-Package.ps1 -GameDir "C:\Games\DungeonSettlers-BepInEx"
```

BepInEx packaging requires the same authorized local frame image under `DungeonSettlers10Slots/Assets`, but no demo save. Shared sources remain in `DungeonSettlers10Slots` and are linked.

Current validation: see [README](README.en.md#validation-and-limitations).
