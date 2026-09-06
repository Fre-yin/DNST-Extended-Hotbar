# Extended Hotbar & Hotbar Helper

[Deutsch](README.md) | **English**

**Extended Hotbar** expands Dungeon Settlers to ten active skill slots, a separate basic attack and three item quickslots. **The optional Hotbar Helper is a Windows application for installing and updating the mod.** The familiar character panel stays in place.

## Also included: Hotbar Helper

**Want to install or update the mod without manually copying mod files?** The optional **Hotbar Helper** for Windows does that for you. **Extended Hotbar 0.3.7 is already bundled** – no separate mod ZIP needed.

- Install or update with a button; existing hotbar files are backed up.
- Undo changes, repair incorrect “Alpha” key labels and check loading status.
- Prepare a separate save copy for the original hotbar and remove Extended Hotbar. **This save conversion is still experimental; try a test campaign first.**
- Controls and help in all ten game languages. Other mods and MelonLoader are retained.

**[Hotbar Helper: overview and instructions →](HotbarHelper/README.en.md)** · [Deutsch](HotbarHelper/README.md)

The helper is optional: manual installation remains available. It does not install MelonLoader or download updates online; future versions need a new helper package.

## Download the mod here

**[Open the downloads section → Releases](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases)**

Open **Extended Hotbar 0.3.7** and scroll down to **Assets**. If that release is not visible yet, its publication has not been completed. Do not substitute the older package for the new game build.

**Do not use “Code → Download ZIP”.** That button and the automatic **Source code (zip/tar.gz)** downloads provide developer source code, not an installable mod.

### Four packages – choose exactly one

| Download | Mod 0.3.7 | Helper 0.1.2 | Demo save |
| --- | --- | --- | --- |
| **Extended-Hotbar-0.3.7.zip** | Yes, manual installation | No | No |
| **Extended-Hotbar-0.3.7-mit-Testspielstand.zip** | Yes, manual installation | No | Yes |
| **Extended-Hotbar-Helper-0.1.2-test.zip** | Yes, bundled in the helper | Yes | No |
| **Extended-Hotbar-Helper-0.1.2-test-mit-Testspielstand.zip** | Yes, bundled in the helper | Yes | Yes |

**You only need one package.** All four contain the same mod; both helper packages already include it. The optional “10 Slots Testfile” save has assigned slots and extra items. **Even with the helper, import it separately using the instructions in the Testspielstand folder – it is not installed automatically.**

`SHA256SUMS.txt` provides optional checksums for all four ZIPs. QA and Frieren are not part of these packages.

## Requirements

- **64-bit Windows** and a legally obtained copy of Dungeon Settlers.
- Supported game version: **DS_B.0.4.19 / Steam build 25154317**.
- **MelonLoader 0.7.3**, installed separately. The game and loader are not bundled.
- The helper additionally requires **.NET Framework 4.8**.

These are **test versions**. Other game builds are not approved. A game update may require a new mod and helper version. Back up your saves before trying them.

## Install or update

### Using the optional helper

1. Extract the **entire helper ZIP** into its own folder, not into `Mods`.
2. Close the game and all test copies. Open `Extended-Hotbar-Helper.exe`.
3. Check the displayed game folder: it must contain `DungeonSettlers.exe`.
4. Choose **Install / update Extended Hotbar**, review the summary and confirm.
5. Start the game yourself through Steam afterwards.

The helper backs up existing hotbar files before replacement. Installation preserves other mods, MelonLoader and your saves. It does not install MelonLoader or download updates online; download a new helper package for future versions.

Controls and help support all ten game languages. Selecting a helper language does not change your saved game language. [Full helper instructions](HotbarHelper/READ%20FIRST%20-%20ENGLISH.txt).

### Without the helper: copy the files yourself

1. Close the game, back up your saves and extract one of the **manual mod ZIPs**.
2. Back up any existing `DungeonSettlers10Slots.dll` or legacy `DungeonSettlers12Slots.dll` **outside** `Mods`. Do not load two hotbar versions together.
3. Copy the included **Mods** folder into the game folder beside `DungeonSettlers.exe`. Merge with an existing Mods folder; do not create `Mods/Mods`. Do not delete other mods.
4. Start the game. Initial MelonLoader setup may take longer; if the game closes afterwards, start it again.

The correct mod package contains `Mods/DungeonSettlers10Slots.dll` and `Mods/DungeonSettlers10SlotsAssets`. `DNST-Extended-Hotbar-main.zip` is the wrong download for installation. [Step-by-step instructions](DungeonSettlers10Slots/UserGuide/READ%20FIRST%20-%20ENGLISH.txt).

Import the demo save separately using its included instructions; do not copy it into the game folder or overwrite your own campaign. The unchanged demo save was saved under **DS_B.0.4.17**; it is not a newly playtested 0.4.19 campaign.

## Play without Extended Hotbar again

The helper's second main button offers to **create a separate save copy for the original hotbar and uninstall Extended Hotbar**. Select a campaign and explicitly confirm the summary.

Your original is preserved. The copy keeps the first four skill assignments, basic attack and one item slot. Learned skills and inventory are not removed. Affected keys return to the original bindings; unrelated settings, mods and MelonLoader remain.

**Save conversion is experimental.** Loading, saving and reloading the converted copy in an actual campaign has not yet been signed off. Try a test campaign first, not your only important save. Original and copy progress independently; progress is never merged. Supported export formats are 0.4.17 and 0.4.19; unknown formats and Ironmode are refused.

The help section contains undo and a separate repair for incorrect “Alpha” key labels. Key-label repair alone does not remove the mod or convert a save. [Details and backups](HotbarHelper/READ%20FIRST%20-%20ENGLISH.txt).

## Default controls

| Action | Keys |
| --- | --- |
| Use skills 1 through 10 | 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 |
| Use item quickslots 1 through 3 | Q, E, R |
| Select characters 1 through 10 | Left Shift + 1 through 0 |

Clicking slots works too. Existing custom bindings are preserved and may differ. Additional bindings can be configured in the game's options; their labels follow the game language. As in the original game, the bar is hidden when no character is selected.

## What changed and what was tested?

**Mod 0.3.7:** Updated for DS_B.0.4.19 / build 25154317. Slot behavior, frame artwork and the demo save are unchanged from 0.3.6.

**Helper 0.1.2:** Includes mod 0.3.7, two clear main actions, a separate selection/confirmation dialog for removal, reorganized help and text in ten languages. A repeated language-switching bug was fixed. Installations with no actual changes do not create misleading undo records.

Automated tests and headless game-interface checks covered slots, persistence, key bindings and ten languages. They do not replace a full visible campaign, combat or long-session playtest on the new build. The experimental helper-converted save is explicitly not signed off by these checks. Translation feedback is welcome.

## Other mods and loaders

**MelonLoader only.** No BepInEx edition is available; running alongside BepInEx mods such as Reroll Helper is unconfirmed. Do not simply install both loaders on top of each other. Compatibility with other MelonLoader mods has not been comprehensively tested either.

## License and game artwork

Independently authored mod/helper code, build scripts and original instructions use the [MIT License](LICENSE).

**The edited original frame artwork is not MIT-licensed.** It is based on Dungeon Settlers artwork, enlarged and adapted by Danny; rights in the original remain with their respective holders. Game code, game content and the optional demo save are also excluded from our code license. This includes the artwork embedded in the helper.

See [LICENSING.txt](LICENSING.txt), the [artwork notice](Assets/NOTICE.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) and [license texts](Licenses/). These notices remain included in all download packages.

This source repository contains no game artwork, game/loader DLLs, compiled mod DLL or actual campaign file. `HotbarHelper/SyntheticSave.json` is an authored test fixture, not a playable save.

Unofficial community project, not an official product of CanOpener or the publisher.

## For developers

[Mod source](DungeonSettlers10Slots/) · [Helper source](HotbarHelper/) · [Mod tests](tests/Hotbar.Tests/) · [Build instructions](BUILDING.en.md). Technical directory and DLL names remain unchanged for compatibility. You do not need to compile anything to play.
