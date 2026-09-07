# Extended Hotbar Helper — prerelease 0.1.11

0.1.11 labels the file picker “Select manually…” (German: “Manuell auswählen…”)
in all ten languages, distinguishing it from automatic search and the discovered
installation dropdown. Both game and profile pickers remain manual; their behavior,
security checks and disabled states are unchanged. Mod remains 0.3.7.

0.1.10 dismisses the obsolete game-search prompt after selecting or entering a
nonempty game path. It changes only that advisory message: actual errors,
diagnostics, operation results and backup information remain visible. The UI
preview regression covers dropdown selection, manual entry, whitespace-only
input, missing-game prompts and preservation of existing errors/results.

0.1.9 detects Dungeon Settlers locally in the background at startup and through
the game-folder Search button. It reads 32/64-bit Steam registry locations,
Steam's libraryfolders.vdf and appmanifest_2798330.acf, including nonstandard
library roots and install directory names. Known Steam folders on local drives
and named game/test containers provide a fallback for standalone test copies.
It does not query the network or recursively scan whole drives. Named backup/
archive branches are skipped by the fallback search, not silently selected.

One match fills an empty, untouched path field. Multiple matches appear in an
editable dropdown with full paths; none is chosen automatically. A user's typed
path, file-picker choice or update handoff is preserved. Searches are cancellable,
do not block the window, and are cancelled before mutation actions. Late results
cannot replace a manually edited path. Installation still requires confirmation
and its unchanged game fingerprints; discovery never runs game executables.

Discovery bounds: 64 Steam libraries, 1 MiB per metadata file, 8192 KeyValues
entries, nesting 16, 512 fallback folders and depth 3 inside each selected
game/test container. Metadata/copy traversal has a five-second cooperative budget;
individual local OS reads cannot be forcibly interrupted. UNC, device, relative,
mapped-network and reparse-point paths are refused. Incomplete game folders and
bad metadata are ignored; manual selection/text entry always remain available.
This is deliberately not an exhaustive search of arbitrary disk locations.

0.1.8 separates the blue action into “Install / update mod” and “Update helper
only”. Helper-only scans only helper ZIPs, needs no valid game/profile path and
opens the new helper without --update-install. It has no mod-install fallback
on missing packages, errors or cancellation. Mod operations retain their game
and backup checks; signatures and the disabled online gate are unchanged.

Download controls are visible in the top toolbar; character bindings have their
own visible main-screen button. The package line shows only
the bundled version unless a newer mod/helper is found. The no-op status is just
“Already up to date.” The German removal action uses the requested vanilla label;
its visible hint retains the same-campaign copy information without the test
sentence. The save conversion itself is unchanged.

0.1.7 fixes installation and save-export failures when an otherwise valid target
path became too long after appending a temporary-file suffix. Temporary files now
use a short unique sibling name; atomic replacement, backups and rollback remain
unchanged. The mod is still 0.3.7 and online updates remain disabled.

Windows Forms helper with local Downloads-folder updates for Extended Hotbar 0.3.7 / DS_B.0.4.19 (25154317). Uses .NET Framework
4.8 and Windows libraries only; no NuGet packages, loader or game assemblies
are needed to compile the helper. The separately verified mod ZIP is embedded.
Its code license does **not** license the game artwork inside that ZIP.

This directory is separate from the game mod. The tested mod DLL and published
release archives are not rebuilt or modified by helper development.

## Build and tests

Requires Windows, the .NET Framework 4.8 Developer Pack, a .NET SDK containing
Roslyn, and PowerShell. Supply the standard 0.3.7 package including its demo,
created by `Prepare-StandardMod.ps1` under `dist/standard-0.3.7`. This verified
documentation-only repack leaves the old release archives intact. It copies the
tested DLL, artwork, demo and legal notices unchanged; only DE/EN instructions
now describe one standard download. Its exact SHA256
is `DA9C92F107298C211799DF678B5EFB267843F8755E84073DA6AB43CDAF6E30B1`.
The hash is checked before embedding; no user-selected DLL is executed. Future
mod builds also include the demo by default and no longer produce two variants.

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
- Export creates an additional manual save in the same campaign, retaining both
  header/clan campaign GUIDs verbatim and learned skills/inventory. Original files
  are backed up, never overwritten. Campaign autosaves are shared; this is stated
  before confirmation. Unexpected references, schemas and Ironmode refuse.
- The user reported a successful in-game save-conversion test. The main hint
  no longer labels it a test. Automated checks do not establish full campaign,
  load/save/reload or unrelated-mod compatibility.
- Load detection validates path, process start/log freshness and explicit activation
  evidence. Monitoring only runs while this helper is open. It never kills/restarts
  the game. Confirmed failure leads to an offer of native bindings after game exit.
- Online connections are disabled at the transport boundary and absent from the GUI.
  Local mod ZIPs are read without running scripts or importing demo saves.
  New packages require a publisher-signed manifest; unknown game builds stay blocked.

## Languages

The helper supports all ten selectable game languages: English, Korean, French,
German, Russian, Simplified Chinese, Traditional Chinese, Japanese, Spanish and
Brazilian Portuguese. Initial language comes from the game's saved settings,
then the Windows UI language, then English. The selector only changes the helper
for this session. It never writes the game's language settings.

All 145 entries per language cover controls, confirmations, status, guarded-error
explanations, built-in help and export display suffixes. Original technical
diagnostics remain separately available. Native Windows file pickers follow the
Windows language. These translations are authored here; native-speaker review
is still welcome.

Tests verify complete key sets, placeholders, all ten game language values,
culture fallback, error rendering and localized export preservation. The optional
`--preview-all <directory>` command renders all languages off-screen with actions
disabled, exercises repeated switching and checks labels/buttons for clipping.
It renders 120 images: main, multiple-game list, selected game, troubleshooting, removal, character keys, explicit
overwrite confirmation, available-update status, mod-update consent and local
helper-update consent, mod/helper choice and helper-only consent for each language.
Default and minimum window sizes are checked. Removal-choice tests verify that
a save must be chosen, optional custom keys require a backup, and selecting
choices never performs file operations. Enter is not an implicit removal default.

User instructions: [Deutsch](BITTE%20ZUERST%20LESEN.txt) /
[English](READ%20FIRST%20-%20ENGLISH.txt). Built-in help is available in all ten
languages; the longer external guides are German and English.

## Package

Run `./Package.ps1` to rebuild, test and create a new local test package under
`../output/Extended-Hotbar-Helper-0.1.11-test`. Existing output is never overwritten.
The package contains the helper, DE/EN guides, ten short language guides, and
unchanged license/artwork notices from the verified embedded mod ZIP. No game,
loader or test executable is distributed with the helper.

There is one standard mod package with the optional demo save, not separate
with/without-save downloads. The same verified complete ZIP is embedded in the
helper EXE; only the three owned mod files are installed. The troubleshooting
action “Save mod package with demo save” writes that ZIP to a user-chosen new file
without extraction or save import. Existing destinations are refused. The user
extracts and imports the demo manually using the instructions inside the ZIP.
The demo is the unchanged DS_B.0.4.17 snapshot, not a fresh 0.4.19 campaign.
Keeping the helper ZIP's existing 21-file layout allows 0.1.6/0.1.7 clients to
validate the new package; the entire archive still needs its signed sidecar.


## Helper 0.1.8: local updates, online disabled

The mod choice scans the Windows Downloads known folder again before installation.
Startup scanning can be disabled separately; it does not download, install or launch
anything. Only top-level, completed Extended-Hotbar-X.Y.Z[-mit-Testspielstand].zip
files are considered (browser duplicate suffixes such as " (1)" are accepted).
Selection is numeric, not based on file dates. Reinstalling the same version is
possible; installing an older version over a newer DLL is refused. Bundled 0.3.7
remains available when there is no local package and no newer installed version.

Original 0.3.7 packages are accepted only when their three installable files match
the embedded release exactly. Future packages need ExtendedHotbar.update.json
inside the ZIP. Its RSA-4096/SHA256 signature binds purpose, repository, mod version,
minimum helper, the known game fingerprints and the three installed file hashes.
Other ZIP entries are never installed or executed. A new game build or save format
still needs a reviewed helper: downloaded metadata cannot override compatibility.

Reads are bounded (16 MiB ZIP, 128 entries, 4 MiB per entry, 32 MiB declared expansion);
unsafe paths, links, duplicate entries and conflicting same-version payloads fail.
Only the top-level Downloads folder is scanned, with a 10000-file limit. The exact
ZIP and signature are rechecked after confirmation. No extraction to game paths
occurs before the existing transactional installer performs its checks and backups.
The selected game may not change during an asynchronous scan.

The dormant GitHub implementation remains for future work, but OnlineEnabled=false
rejects requests before URI parsing or socket creation. There is no user option or
downloaded setting that enables it. TLS targeting was corrected via FrameworkTarget.cs:
direct Roslyn builds previously omitted .NET Framework 4.8 metadata, triggering old
TLS defaults. The new regression test failed before the fix and passes afterwards.

Package.ps1 does not read the private publisher key by default. -SignUpdatePackage
is an explicit publisher-only option for signed local helper packages. No secret is bundled.
See LOCAL-UPDATES-PUBLISHING.md for preparing future signed mod ZIPs. Hardware-key
migration, key recovery and public online-update testing remain deferred.

## Local self-updates

LocalHelpers scans the same folder for newer helper ZIPs, with priority over mod
ZIPs. It requires the matching ZIPNAME.zip.update.json alongside the ZIP. Both are
read locally; URLs in the reconstructed identity are never requested. The original
publisher-signature, expiry, complete package hash/size/layout, EXE identity/version
and pre-launch file verification remain enforced. Missing/invalid signatures refuse.
The new helper is staged side by side and launched only after confirmation, with
Windows attachment checks retained. For the mod choice, the game must be closed;
selected folders are passed as bounded data and the new helper asks again before
mod installation. Helper-only opens normally without that handoff and never
installs the mod. Its update does not require the game to be installed or closed.
Existing shortcuts and the old helper are not replaced. A ZIP alone cannot authorize
execution. Historical helper variants named `-mit-Testspielstand` are not update
inputs; the new standard helper embeds the demo without changing its ZIP layout.
OnlineEnabled remains
false throughout. Unit tests suppress execution. The optional integration probe
starts the actual signed helper in its read-only preview mode and installs the
local mod into an isolated fixture with real game-build checks. Interactive
update consent, restart handoff and in-game play remain separate acceptance steps.

The optional probe is test-only (not shipped in the user ZIP):

```powershell
./bin/Helper.Tests.exe --smoke-local-updates <signed-package-folder> <read-only-source-game> <test-output-root> <save-fixture-json>
```

It creates a unique test directory, copies only four game/loader files needed for
validation and a save fixture, and never launches the game or modifies the source
installation/profile. Windows attachment prompts must not be bypassed. The helper
process is given a 30-second preview deadline; a pending Windows prompt needs manual
inspection. Outputs are retained for inspection, not included in release archives.
