> Helper 0.1.13 source update: the manual job still requires both unchanged pinned mod packages from helper-v0.1.12 (MelonLoader 0.3.8 and BepInEx 0.3.8-bepinex.1). The workflow and its package hashes are unchanged. A missing or altered asset stops the build. The all-source job remains independent and retains its documented limitations. Historical counts and local results below describe the original workflow preparation, not the current source inventory or a new hosted scan.

# CodeQL analysis boundaries

## Why a successful scan reported low quality

At commit `dd5815920d8d8e3d5c50dce85937ab31feee15fa`, GitHub's default
setup ran C# with `build-mode: none` on `ubuntu-latest`. The reported 70% of
calls with targets and 81% of expressions with known types were below the
85% guidelines. This is an analysis-quality diagnostic, not by itself a
vulnerability finding or evidence of a failing game mod.

Repository evidence explains important gaps:

- `HotbarHelper/Build.ps1` invokes Roslyn directly for a .NET Framework 4.8
  Windows Forms executable and a separate test executable. There is no helper
  `.csproj`. A no-build scan cannot obtain those explicit references and
  compilation boundaries from an MSBuild project.
- `DungeonSettlers10Slots/DungeonSettlers10Slots.csproj` requires `GameDir`
  and 15 local assembly references, including generated IL2CPP, Unity,
  MelonLoader and game assemblies. These are intentionally absent from the
  repository and a hosted runner. `autobuild` alone cannot supply them.
- `NuGet.Config` clears package sources. This is deliberate for these local
  builds, but means missing framework reference packs must already be installed.
  Opening a public feed would not provide the private game assemblies.
- `tests/Hotbar.Tests/LocalizationTestDoubles.cs` deliberately substitutes some
  production type names within a separate test assembly. The no-build extractor
  combines sources heuristically; these names and mixed framework targets are
  additional risks to resolution. They must not be treated as real native APIs.

The exact contribution of each gap to the percentages has not been measured.
The run metadata was public, but the anonymous log-download endpoint returned
HTTP 403. This diagnosis does not claim inspection of those private logs.

## Two complementary scans

The advanced workflow `.github/workflows/codeql.yml` has two independent jobs:

| Job / result category | Coverage and limitations |
| --- | --- |
| All sources: `/language:csharp` | Keeps `none` analysis of the entire source tree, including the native mod. Installs .NET 6 SDK reference packs. Missing game dependencies can still cause the original warning. |
| Manual build: `/language:csharp/build:helper-and-core` | Builds the real Windows helper, its isolated tests and the independent core test project. Compiler references and separate assemblies are captured. Does **not** cover the complete native mod integration. |

There are no path exclusions, suppressed quality diagnostics or lowered
thresholds. A good score in the second job is not proof that the first job's
dependency gaps were fixed. Check both configurations after uploading.

At the original reviewed commit, the manual commands compiled 26 of the 48
tracked C# files: all 18 helper files, three core test files and five linked mod files
(`ItemSlotState`, `ItemSlotSaveCodec`, `BoundedSnapshotCache`, `SlotText`,
`NativeLocalization`). The other 22 mod files retained no-build coverage only.
`NativeLocalization` used the test doubles in this compilation, not real game
bindings. These historical figures are build-input counts; actual extraction coverage must be
checked in CodeQL's output.

The manual job invokes the repository's `HotbarHelper/Build.ps1`, including its
resource verification and tests. It downloads the two pinned mod ZIPs from
`helper-v0.1.12`, checks both SHA256 hashes before the build, and keeps them
outside the checkout. It does not execute or install either mod, import demo
saves, use a private signing key or modify releases. The public release assets
must remain accessible; a missing or changed asset stops the build.

Helper 0.1.13 removes the program's HTTP client and release-feed functions.
This does not remove the workflow's resource downloads. The helper retains
local package and signature validation. `LocalOnlyChecks.cs` belongs only to
the test executable and checks the built helper for the former online components
and network API references. These checks do not establish antivirus approval
or improve CodeQL analysis-quality percentages by themselves.

The Windows runner supplies .NET Framework 4.8 reference assemblies. SDK 6 is
installed for the existing `net6.0` target; SDK 10 supplies the compiler.
This does not migrate the mod's runtime. The test rebuild disables incremental
and shared compilation so CodeQL can observe the compiler. Helper test output
uses a short temporary path because .NET Framework tests create nested folders.
Only compiler/test executables run, not the GUI helper or the game.

## Initial activation and ongoing acceptance

The activation instructions below describe the original workflow setup. For
the Helper 0.1.13 source update, preserve the existing advanced workflow and
check the new run for the complete source commit; do not repeat setup or
disable scanning just to upload updated helper files.

1. Prepare both repository files, preserving `.github/workflows/codeql.yml`.
2. In repository **Settings > Advanced Security > CodeQL analysis**, use
   **Switch to advanced**. GitHub asks to disable the old *default setup* as
   part of that switch. Do not leave it disabled: commit the prepared workflow
   immediately. Do not also commit a generated template or leave another CodeQL
   workflow alongside this one. Default setup can reject advanced SARIF uploads.
3. Upload/commit these files yourself to `main`. Do not move release tags or
   replace release ZIPs. On push the workflow should start; it also has a manual
   **Run workflow** action and a weekly scan.
4. Require both jobs to complete. Check the helper build/test output, core test
   output, the manual extraction's file coverage and both result categories under
   code scanning's tool status. In particular, check `MainForm.cs`, `Operations.cs`,
   `Updates.cs` and `UpdateSignature.cs` are present in the manual analysis.
5. Compare analysis quality per category. Do not present the new percentages as
   verified until a real CodeQL run has produced them. A continuing warning in
   the all-source job is an explicitly retained limitation, not a reason to hide it.

For rollback, disable/remove this advanced workflow and re-enable default setup;
do not leave the repository without a scanner. Review and update the pinned
official action commits periodically.

Full compiler-aware analysis of the native mod needs a separate trusted local
environment with legally available, initialized game references, following
`BUILDING.md`. It is not configured here. Do not upload game DLLs, put them in
public build artifacts, add fictitious API stubs to inflate coverage, or expose a
personal gaming PC as a public pull-request runner.

## Historical local preparation checks

During the original workflow preparation on Windows, the helper build passed
all 207 isolated checks and the core project passed all 32 tests. The proposed
workflow passed actionlint 1.7.12.
The native mod's reference-validation target failed as expected without
`GameDir`, confirming that merely selecting `autobuild` is insufficient.
No local CodeQL CLI database or new GitHub analysis was run during preparation;
neither improved percentages nor successful hosted extraction were established
by those preparation checks. The current Helper package's documented test
status is maintained in [README](README.en.md#validation-and-limitations).

## Deutsch – Kurzfassung

Der Gesamtscan bleibt erhalten. Zusätzlich wird der echte Windows-Helper samt
isolierten Tests und den spielunabhängigen Modtests kompiliert und analysiert.
Die Warnung des Gesamtscans kann wegen fehlender Spiel-DLLs weiterhin erscheinen.
Der zusätzliche Scan darf nicht als vollständige Analyse der nativen Mod gelten.
Es werden keine Spielfunktionen, Sicherheitsprüfungen oder Release-Dateien geändert.
Für den Helper-0.1.13-Quellabgleich bleibt der bestehende Advanced-Workflow
erhalten. Entscheidend ist der neue Lauf für den vollständigen Quellstand;
die historischen Zahlen in diesem Dokument sind kein aktuelles Prüfergebnis.

## References

- [Original scan](https://github.com/Fre-yin/DNST-Extended-Hotbar/actions/runs/34144957433)
- [GitHub: C# build modes, dependency inference and compiler tracing](https://docs.github.com/en/code-security/reference/code-scanning/codeql/build-options-for-compiled-languages)
- [GitHub: manual builds analyze the sources actually built](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/manage-your-configuration/codeql-for-compiled-languages)
- [GitHub: switching to advanced setup](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/configure-code-scanning/configuring-advanced-setup-for-code-scanning)
- [GitHub: default setup blocks advanced CodeQL uploads](https://docs.github.com/en/code-security/reference/code-scanning/sarif-files/troubleshoot-sarif-uploads/default-setup-enabled)
- [Official Windows 2022 runner inventory](https://github.com/actions/runner-images/blob/main/images/windows/Windows2022-Readme.md)
