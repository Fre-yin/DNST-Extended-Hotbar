# Extended Hotbar Helper — prerelease 0.1.12

Helper 0.1.12 includes Hotbar 0.3.8 for MelonLoader and the separate BepInEx
0.3.8-bepinex.1 preview. Select the game copy, check its loader, then confirm
installation. A uniquely detected loader is preselected. Missing, wrong or mixed
loaders are refused; the Helper never installs or removes a loader.

## Bindings

Installation never changes keys or resurrects an old binding profile. The mod
preserves the game's native defaults and adds unbound rows for skills 5–10 and
items 2–3 in both columns. Existing saved bindings remain, including old
conflicts: their intent cannot be inferred safely.

Players can bind their own keys in game options. The separate, optional Helper
profile sets skills 1–0, characters Left Shift + 1–0, items Q/E/R, with additive
selection unbound. It replaces both columns for those actions. Other occupied
keys are listed and require a second, explicit overwrite confirmation. Cancelling
makes no change. Preview consent is bound to the settings hash; backups and
scoped undo retain unrelated later settings. The separate character-only profiles
remain available. No command-line option in the mod rewrites user settings.

## Module boundaries

| Responsibility | Source |
| --- | --- |
| Loader layout, detection, file ownership, package names and signature purpose | ModLoaders.cs |
| Release fingerprints, pinned embedded archives, safe package read/export | ReleaseInfo.cs |
| Optional full Hotbar key map | HotbarKeyProfile.cs |
| Lossless binding edits, conflict checks, scoped restoration | Profiles.cs |
| Stopped-game guards, atomic file transactions, backup and rollback | Operations.cs |
| Loader-specific local update selection and verification | LocalMods.cs |
| Current-process activation evidence for each loader | LoadStatus.cs |
| User choices and confirmations, no file-copy logic | MainForm.cs |

Both mods link the same game logic in ../DungeonSettlers10Slots; their loader
entry points stay separate. Do not copy a MelonLoader DLL into BepInEx or vice versa.

## Build and tests

Requires Windows x64, .NET Framework 4.8 Developer Pack, a .NET SDK with Roslyn,
and PowerShell. The Helper itself needs no game assemblies or NuGet packages.
Build the two verified mod packages first, then:

```powershell
./Build.ps1 -OutputDirectory ./bin-0.1.12
```

Default inputs:
- ../DungeonSettlers10Slots/dist/Extended-Hotbar-0.3.8.zip
- ../DungeonSettlersHotbar.BepInEx/dist/Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip

Use -PackagePath and -BepInExPackagePath to supply those exact archives elsewhere.
Build.ps1 and the runtime both check their pinned SHA256 values. A new release
must update both pins deliberately; do not disable verification to accept a ZIP.
Prepare-StandardMod.ps1 is a historical 0.3.7 repack utility, not the current build.

Tests create isolated synthetic game/profile/backup folders under the output
directory. Use a short output path to keep the tests' nested fixtures below
Windows path limits. Tests do not operate on your live profile. Optional
-ReadOnlyGameCheck validates actual game fingerprints without modifying a game.

`Extended-Hotbar-Helper.exe --preview-all <output-directory>` renders read-only
off-screen UI states in ten languages, including loader choice, optional profiles,
conflict confirmation, removal and updates. Native-speaker translation review and
interactive gameplay tests remain useful beyond automated checks.

## Safety model and limitations

- Supported game: DS_B.0.4.19 / Steam build 25154317, exact game-code and metadata
  hashes. Loaders: MelonLoader 0.7.3 or BepInEx 6 Unity IL2CPP x64, tested be.788.
  BepInEx 5 and Mono are not supported.
- Separate game copies normally share the same real save/settings profile.
  Never run both copies together. A game copy is not a save backup.
- Installation touches only the selected loader's three Hotbar payload files
  (plus removal of the known legacy DLL for MelonLoader). Other mods are retained.
- No recursive game deletion; no arbitrary archive extraction, script execution,
  loader switch, network request, administrator request or automatic game launch.
- Local paths, reparse-point refusal, verified backups, optimistic conflict checks,
  transaction locking and compensating rollback remain mandatory.
- All game processes must be closed. Checks repeat before writes. These checks
  cannot atomically prevent an independent process launch racing a filesystem write.
- Undo never rolls back campaign progress. Changed owned files or affected keys
  block restoration; interrupted operations must be recovered first.
- Vanilla export remains experimental: a new manual save in the SAME campaign,
  original retained, shared autosaves. Ironmode and unknown schemas are refused.
- BepInEx activation requires an exact process ID/start-time/game-path marker and
  successful lifecycle attachment. Old plugins lacking the marker stay Unknown.
  The known BepInEx Class::Init warning is not suppressed.

## Package

`./Package.ps1` builds/tests and creates a new output/Extended-Hotbar-Helper-0.1.12-test
folder and ZIP. Existing release outputs are never overwritten. It uses the
separate bin-0.1.12 build directory, leaving older helper executables intact.

The outer ZIP retains the 21-file update layout understood by older helpers.
Both editions' legal notices are included in the existing notice files. No game,
loader, private keys or test executables are distributed. Only the MelonLoader
embedded ZIP contains the optional unchanged DS_B.0.4.17 demo save; BepInEx does
not. Export saves the selected loader's ZIP to a new user-chosen file and never
imports a save. Game artwork is not covered by the code's MIT license.

## Local updates

Scans only the top-level Windows Downloads folder. Selection is loader-specific;
the other loader's ZIP is not an update candidate. Same-version installs are
allowed, downgrades are refused. Future packages need a verified publisher-signed
manifest; the current bundled payload can also be recognized by exact hashes.
MelonLoader signatures use ExtendedHotbar.Mod.v1, BepInEx signatures use
ExtendedHotbar.Mod.BepInEx.v1. Signatures bind the expected three paths/hashes,
numeric mod version, repository, minimum Helper and game fingerprints.

Archive size/count/path/link limits, duplicate checks and confirmation-time
revalidation stay enforced. A game or loader selection change invalidates an
in-flight scan. Helper-only updates remain independent of mod/game selection.

Online updates remain disabled. Package.ps1 does not use the private signing key
unless -SignUpdatePackage is explicitly supplied. The local unsigned package can
be extracted and started manually; old helpers need its valid signed sidecar for
self-update. See [publishing instructions](LOCAL-UPDATES-PUBLISHING.md).

User guides: [Deutsch](BITTE%20ZUERST%20LESEN.txt) /
[English](READ%20FIRST%20-%20ENGLISH.txt). Instructions and built-in help cover all
ten game languages. GitHub uploads and commits remain manual.
