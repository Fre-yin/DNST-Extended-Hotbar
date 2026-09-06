# Extended Hotbar Helper — local test build 0.1.2

Offline Windows Forms helper for Extended Hotbar 0.3.7 / DS_B.0.4.19 (25154317). Uses .NET Framework
4.8 and Windows libraries only; no NuGet packages, loader or game assemblies
are needed to compile the helper. The separately verified mod ZIP is embedded.
Its code license does **not** license the game artwork inside that ZIP.

This directory is separate from the game mod. The tested mod DLL and published
release archives are not rebuilt or modified by helper development.

## Build and tests

Use a short checkout path, such as `C:\Dev\ExtendedHotbar`, because nested
isolated-test directories can otherwise exceed the Windows/.NET Framework
path-length limit. `Build.ps1` accepts a short `-OutputDirectory` as an alternative.
`Package.ps1` builds under this directory's `bin`; use a short checkout for it.

Requires Windows, the .NET Framework 4.8 Developer Pack, a .NET SDK containing
Roslyn, and PowerShell. Obtain the unchanged `Extended-Hotbar-0.3.7.zip`
installation package from the same release. Its exact hash is checked before
embedding; a locally repacked ZIP will not match. No user-selected DLL is executed.

```powershell
./Build.ps1 -PackagePath 'C:\Downloads\Extended-Hotbar-0.3.7.zip'
```

This compiles the helper and runs isolated tests against the authored synthetic
JSON fixture. For additional real-save coverage, supply a COPY with
`-TestSavePath 'C:\Tests\10SlotsTestfile.json'`. Optionally use
`-ReadOnlyGameCheck 'C:\Games\Dungeon Settlers'` to verify actual fingerprints
and read the running game's load status, without changing that installation.
Test files are created only below the selected output directory's `test-runs`.
Tests do not use your live save/settings directory. A directory-junction test
creates its junction only inside that test directory. Test outputs are retained.

## Safety model and limitations

- Four fixed owned game paths; no recursive game-folder deletion or loader edits.
- Local absolute paths only; reparse-point targets and ambiguous paths refused.
- Per-file atomic replacement, verified before/after backups, conflict checks,
  operation lock, compensating rollback and persistent interrupted-operation state.
- All DungeonSettlers processes must be closed for mutations, including test copies.
  Checks repeat at each write; they are not an OS-level guarantee against an
  independently launched process racing a filesystem operation. A detected start
  stops mutation/recovery until the game is closed.
- A strict, bounded lossless JSON editor changes selected spans only, retaining
  unrelated numeric precision, serializer metadata and fields verbatim.
- Key profiles are scoped to native selection/hotbar bindings and mod-owned IDs.
  No whole-settings rollback when returning after playing; changed hotkeys refuse
  automatic restoration. Unrelated preferences remain current.
- Export creates a separate save with new header/clan campaign GUIDs and retains
  learned skills/inventory. Unexpected references, schemas and Ironmode refuse.
- Export is **not yet in-game validated**. All relevant GUI flows label it a test.
  No claim is made that native load/save/reload or unrelated mods are fully tested.
- Load detection validates path, process start/log freshness and explicit activation
  evidence. Monitoring only runs while this helper is open. It never kills/restarts
  the game. Confirmed failure leads to an offer of native bindings after game exit.
- No online update feed. To ship a later mod, update the reviewed release catalog,
  package hash, build inputs and documentation together. Unknown builds stay blocked.

## Languages

The helper supports all ten selectable game languages: English, Korean, French,
German, Russian, Simplified Chinese, Traditional Chinese, Japanese, Spanish and
Brazilian Portuguese. Initial language comes from the game's saved settings,
then the Windows UI language, then English. The selector only changes the helper
for this session. It never writes the game's language settings.

All 85 entries per language cover controls, confirmations, status, guarded-error
explanations, built-in help and export display suffixes. Original technical
diagnostics remain separately available. Native Windows file pickers follow the
Windows language. These translations are authored here; native-speaker review
is still welcome.

Tests verify complete key sets, placeholders, all ten game language values,
culture fallback, error rendering and localized export preservation. The optional
`--preview-all <directory>` command renders all languages off-screen with actions
disabled, exercises repeated switching and checks labels/buttons for clipping.
It renders 30 images: main, troubleshooting and removal for each language.
Default and minimum window sizes are checked. Removal-choice tests verify that
a save must be chosen, optional custom keys require a backup, and selecting
choices never performs file operations. Enter is not an implicit removal default.

User instructions: [Deutsch](BITTE%20ZUERST%20LESEN.txt) /
[English](READ%20FIRST%20-%20ENGLISH.txt). Built-in help is available in all ten
languages; the longer external guides are German and English.

## Package

Run `./Package.ps1` to rebuild, test and create a new local test package under
`../output/Extended-Hotbar-Helper-0.1.2-test`. Existing output is never overwritten.
The package contains the helper, DE/EN guides, ten short language guides, and
unchanged license/artwork notices from the verified embedded mod ZIP. No game,
loader, source save or test executable is distributed with the helper.


## Helper 0.1.2 changes

Two main actions: install/update, or prepare a separate original-hotbar save
and uninstall Extended Hotbar. The latter has a dedicated selection/summary
dialog. Undo, key-label repair and profile selection are collapsed into
troubleshooting (opened automatically for pending recovery). Backup action IDs
stay unchanged; multiline action labels are flattened in historical lists.

UI fonts are cached by family for the lifetime of the form. WinForms can retain
an equal font rather than assign its new instance; disposing the previous font
on every language switch left the active font unusable. The preview test first
reproduced this failure and now asserts that the active Font.Height stays valid.

The helper carries hotbar 0.3.7 and guards the exact 25154317 binary/metadata pair.
Export accepts the verified 0.4.17 and 0.4.19 save layouts, preserving the original
header version. 464 save/key fields matched between native catalogs. A new
synthetic 0.4.19-header test verifies unrelated bytes and rejects future versions.
This does not substitute for an in-game campaign load/save/reload of the export.

## Helper 0.1.1 changes (retained)

Identical installs and already-original key profiles are successful no-ops: no
new transaction folder and no misleading corrupt-backup error. Legacy 0.1.0
no-change records remain untouched, are omitted from the Undo menu and cannot
hide the last real native-key change when reinstalling. Interrupted recovery
records remain visible. An old no-change record can safely return no changes.

User-facing action names now distinguish switching back to the vanilla hotbar
(with a separate test save) from resetting only affected key bindings. Stored
transaction action identifiers are unchanged for backup compatibility.
