# Extended Hotbar & Hotbar Helper

[Deutsch](README.md) | **English**

Ten active skill slots, a separate basic attack and three item slots for Dungeon Settlers. The optional Helper handles installation, local updates and optional key profiles.

**Prepared prerelease: Helper 0.1.13 · MelonLoader mod 0.3.8 · BepInEx mod 0.3.8-bepinex.1**

## Downloads

Open [Releases](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases) and check the files offered under **Assets**. Helper 0.1.13 is prepared for publication; until its package is offered there, only earlier Helper versions are available. Both mod packages remain unchanged. Source-code archives are not installable packages.

| Purpose | File |
| --- | --- |
| Prepared Helper package; includes both mod editions | **Extended-Hotbar-Helper-0.1.13-test.zip** |
| Manual MelonLoader installation | **Extended-Hotbar-0.3.8.zip** |
| Manual BepInEx installation | **Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip** |
| Optional checksums | **SHA256SUMS.txt** |

Choose the Helper ZIP or the mod ZIP matching your loader. Only the MelonLoader mod package contains the optional DS_B.0.4.17 demo save. The Helper can export the selected embedded package. No save is imported automatically.

## Requirements and start

- Windows x64; Dungeon Settlers **DS_B.0.4.19 / Steam build 25154317**.
- Separately installed **MelonLoader 0.7.3** or **BepInEx 6 Unity IL2CPP x64**, tested with **6.0.0-be.788+5b766a3**. BepInEx 5 and Mono are unsupported. Never mix loaders in one game copy.
- Helper: **.NET Framework 4.8**. Neither game nor loader is bundled or installed by the Helper.

1. Back up saves and close all game instances. Game copies normally share saves and settings; a game copy is not a save backup.
2. Extract the entire Helper ZIP to its own folder and open `Extended-Hotbar-Helper.exe`.
3. Check the selected game folder and loader, then confirm installation. Missing, wrong or mixed loaders are refused.
4. Start the game yourself. Other mods remain untouched.

For manual installation, merge the package's `Mods` folder (MelonLoader) or `BepInEx` folder into the game directory. Keep the DLL and its supplied artwork folder together. Back up previous Hotbar files outside the mod folder before updating; follow the packaged instructions.

## Optional key profile

Installation preserves bindings and native defaults. Additional skill/item actions start unbound in both columns. Existing bindings, including old conflicts, are not silently changed.

Bind keys in game options or explicitly apply the Helper profile: skills **1–0**, characters **Left Shift + 1–0**, items **Q/E/R**, additive selection unbound. It replaces both columns for these actions. Other occupied actions require an additional conflict confirmation. Preview, backup and scoped undo are provided; character-only profiles remain available.

## Local updates

Helper 0.1.13 works locally only. The HTTP client, release feed and online update functions have been removed. Download packages yourself through your chosen mod platform. Optional scanning for packages already present in Windows Downloads remains available.

A local self-update requires the new Helper ZIP and a valid, exactly matching signed `.zip.update.json` together in Windows Downloads. No such signature file has been created for the prepared 0.1.13 package yet. Instead, extract the entire ZIP manually to its own folder once it is available, and start the Helper there. A signature file from an older package is not a replacement.

Helper-only updates do not install a mod. For a validly signed local self-update, the new EXE is stored under `%LOCALAPPDATA%\ExtendedHotbarHelper\Updates`; previous EXEs and shortcuts remain. BepInEx support has been included since Helper 0.1.12.

Checksums are not a signature replacement. Package signing is not Windows Authenticode; Windows launch warnings may still appear.

## Validation and limitations

The complete Helper 0.1.13 source was rebuilt and packaged locally on 2026-09-11: 221 tests passed, 0 failed. Package preparation on 2026-09-10 also documents 150 UI previews across ten languages. Tests cover local package selection, signatures, settings and removal of the former network components, among other checks. This is not approval by antivirus scanners or mod platforms, or the result of a new GitHub check of 0.1.13.

Earlier validation of the unchanged mod packages documents 33 mod tests including the supplied save fixture and both loader builds without warnings/errors. Danny's successful Steam key-configuration and other-function tests on 2026-09-10 used the previous Helper 0.1.12; no new in-game test of 0.1.13 was performed. This does not establish full campaign, long-session or third-party-mod compatibility. The unchanged ZIP instructions partly reflect the earlier, more conservative test status.

The BepInEx message `Class::Init signatures have been exhausted, using a substitute!` remains visible; documented tests pass despite it. Unknown game builds remain blocked.

Experimental return to vanilla creates an additional manual save **in the same campaign** and removes Extended Hotbar. The original is preserved and backed up; campaign autosaves are shared. Other mods and the loader remain installed.

[Helper guide](HotbarHelper/READ%20FIRST%20-%20ENGLISH.txt) · [MelonLoader guide](DungeonSettlers10Slots/UserGuide/READ%20FIRST%20-%20ENGLISH.txt) · [BepInEx guide](DungeonSettlersHotbar.BepInEx/README.md) · [Building](BUILDING.en.md)

Unofficial community project, not an official CanOpener or publisher product. Original code/instructions: MIT; game artwork, demo saves and game content are excluded. See [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) and [artwork notice](Assets/NOTICE.txt).
