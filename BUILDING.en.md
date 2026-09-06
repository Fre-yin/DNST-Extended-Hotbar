# Building Extended Hotbar

[Deutsch](BUILDING.md) | **English** | [Project description](README.en.md)

This guide is for developers. Players will use the ready-to-install release package once available; they will not need these steps.

## Requirements

- Windows x64 and your own installation of Dungeon Settlers **DS_B.0.4.17 / Steam 25143510**.
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
pwsh -File .\DungeonSettlers10Slots\Build-Release.ps1 -GameDir "C:\Games\Dungeon Settlers" -PackageRevision 2
```

The script builds the DLL, runs the tests and creates a verified ZIP under `DungeonSettlers10Slots/dist`. Existing output is never overwritten. `-PackageRevision` identifies the package revision, not a new mod version. Use an unused revision number for another package.

Add `-IncludeTestSave` to include the prepared demo save. You must explicitly place it at `DungeonSettlers10Slots/TestSave/Saves/10SlotsTestfile.json`. Git ignores this folder too. The script does not scan or modify personal save folders. If the frame or an explicitly requested demo save is missing, it stops before building the package.

The exclusions in `.gitignore` apply to Git, not manual browser uploads. Do not upload generated build directories, locally added artwork or save files alongside the source through the GitHub website.

The source remains at version **0.3.6** and still checks the supported game build's fingerprints. Do not simply change that check to enable an unknown game update: interfaces and behavior must be verified again first.
