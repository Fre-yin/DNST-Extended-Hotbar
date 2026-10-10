# CodeQL analysis boundaries

## Why the all-source scan can report low quality

At commit `dd5815920d8d8e3d5c50dce85937ab31feee15fa`, GitHub's default
setup ran C# with `build-mode: none` on `ubuntu-latest`. The reported 70% of
calls with targets and 81% of expressions with known types were below the
85% guidelines. This is an analysis-quality diagnostic, not by itself a
vulnerability finding or evidence of a failing game mod.

Repository evidence explains important gaps:

- `DungeonSettlers10Slots/DungeonSettlers10Slots.csproj` requires `GameDir`
  and 15 local assembly references, including generated IL2CPP, Unity,
  MelonLoader and game assemblies. These are intentionally absent from the
  repository and a hosted runner. `autobuild` alone cannot supply them.
- `NuGet.Config` clears package sources. This is deliberate for these local
  builds, but means missing framework reference packs must already be installed.
  Opening a public feed would not provide the private game assemblies.
- `tests/Hotbar.Tests/LocalizationTestDoubles.cs` deliberately substitutes some
  production type names within a separate test assembly. The no-build extractor
  combines sources heuristically; these names are an additional risk to
  resolution. They must not be treated as real native APIs.

The exact contribution of each gap to the percentages has not been measured.

## Two complementary scans

The advanced workflow `.github/workflows/codeql.yml` has two independent jobs:

| Job / result category | Coverage and limitations |
| --- | --- |
| All sources: `/language:csharp` | Keeps `none` analysis of the entire source tree, including the native mod. Installs .NET 6 SDK reference packs. Missing game dependencies can still cause the warning above. |
| Manual build: `/language:csharp/build:core-tests` | Builds and runs the game-independent test project `tests/Hotbar.Tests`, which compiles five linked mod files (`ItemSlotState`, `ItemSlotSaveCodec`, `BoundedSnapshotCache`, `SlotText`, `NativeLocalization`). Compiler references are captured. Does **not** cover the complete native mod integration. |

There are no path exclusions, suppressed quality diagnostics or lowered
thresholds. A good score in the second job is not proof that the first job's
dependency gaps were fixed. Check both configurations after each upload.
`NativeLocalization` uses the test doubles in the manual build, not real game bindings.

Up to Extended Hotbar 1.0.1 the manual job also built the Extended Hotbar Helper
(category `/language:csharp/build:helper-and-core`). The Helper is no longer
developed because Dungeon Settlers Delvers Core took over its functions. With
Extended Hotbar 1.0.2 its source left the repository and the job was renamed.
Results of the retired category can be removed under code scanning's tool status.

## Ongoing acceptance

1. Keep `.github/workflows/codeql.yml` as the only CodeQL workflow. Default setup
   can reject advanced SARIF uploads, so it stays disabled.
2. Upload or commit changes yourself to `main`. Do not move release tags or
   replace release ZIPs. On push the workflow starts; it also has a manual
   **Run workflow** action and a weekly scan.
3. Require both jobs to complete and check the core test output and both result
   categories under code scanning's tool status.
4. A continuing warning in the all-source job is an explicitly retained
   limitation, not a reason to hide it.

For rollback, disable or remove this advanced workflow and re-enable default setup;
do not leave the repository without a scanner. Review and update the pinned
official action commits periodically.

Full compiler-aware analysis of the native mod needs a separate trusted local
environment with legally available, initialized game references, following
`BUILDING.md`. It is not configured here. Do not upload game DLLs, put them in
public build artifacts, add fictitious API stubs to inflate coverage, or expose a
personal gaming PC as a public pull-request runner.

## Deutsch: Kurzfassung

Der Gesamtscan bleibt erhalten. Zusätzlich werden die spielunabhängigen Modtests
kompiliert, ausgeführt und analysiert. Die Warnung des Gesamtscans kann wegen
fehlender Spiel-DLLs weiterhin erscheinen. Der zusätzliche Scan gilt nicht als
vollständige Analyse der nativen Mod. Bis Version 1.0.1 hat dieser Job auch den
Extended Hotbar Helper gebaut. Der Helper wird nicht mehr entwickelt, weil
Dungeon Settlers Delvers Core seine Aufgaben übernommen hat; mit 1.0.2 ist sein
Quellcode aus dem Repository entfernt.

## References

- [Original scan](https://github.com/Fre-yin/DNST-Extended-Hotbar/actions/runs/34144957433)
- [GitHub: C# build modes, dependency inference and compiler tracing](https://docs.github.com/en/code-security/reference/code-scanning/codeql/build-options-for-compiled-languages)
- [GitHub: manual builds analyze the sources actually built](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/manage-your-configuration/codeql-for-compiled-languages)
- [GitHub: switching to advanced setup](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/configure-code-scanning/configuring-advanced-setup-for-code-scanning)
- [GitHub: default setup blocks advanced CodeQL uploads](https://docs.github.com/en/code-security/reference/code-scanning/sarif-files/troubleshoot-sarif-uploads/default-setup-enabled)
- [Official Windows 2022 runner inventory](https://github.com/actions/runner-images/blob/main/images/windows/Windows2022-Readme.md)
