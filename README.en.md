# Extended Hotbar

[Deutsch](README.md) | **English**

More room for skills and items in **Dungeon Settlers** — an unofficial community mod for MelonLoader.

## What does the mod do?

- Ten active skill slots, with the basic attack kept as a separate slot.
- Three item quickslots next to the skill bar.
- Use skills and items by clicking their slots or pressing the assigned keys.
- Keep the familiar character panel with status, skills and inventory.
- Configure the additional key bindings in the game's options. Labels follow the selected game language.

## Current status

This repository contains the mod source code, tests, instructions and licensing notices. The first release is being prepared. Once released, installation packages will be available in the [downloads section](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases).

The current test version is **0.3.6**, package revision **r2**. This revision only updates instructions and licensing notices; it does not change the mod's functionality.

Please back up your saves before trying any mod. Extended Hotbar is still a test version.

## Download and installation

Choose one of these two packages in the downloads section:

- `Extended-Hotbar-0.3.6-r2.zip`: the mod for your own campaign.
- `Extended-Hotbar-0.3.6-r2-mit-Testspielstand.zip`: the same mod plus an optional demo save with assigned slots and extra items.

You only need one. GitHub's automatic **Source code (zip/tar.gz)** downloads contain the source, not the ready-to-install mod package.

Install MelonLoader 0.7.3 separately, extract your chosen mod ZIP and copy its `Mods` folder beside `DungeonSettlers.exe`. A detailed [step-by-step guide](DungeonSettlers10Slots/UserGuide/READ%20FIRST%20-%20ENGLISH.txt) is also included in the package. Import the optional save separately, following its included instructions.

## Requirements

- A legally obtained copy of Dungeon Settlers for Windows (64-bit).
- Supported test build: **DS_B.0.4.17**, Steam build **25143510**.
- **MelonLoader 0.7.3**, installed separately.

Compatibility with other game versions has not been confirmed. A game update may also require an update to the mod.

## Default controls

| Action | Keys |
| --- | --- |
| Use skill 1 through 10 | 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 |
| Use item quickslot 1 through 3 | Q, E, R |
| Select character 1 through 10 | Left Shift + 1 through 0 |

Existing custom bindings are preserved, so yours may differ from this table. You can change them in the game's options. As in the original game, the bar is hidden when no character is selected.

## Other mods and loaders

This release supports **MelonLoader**. There is currently no BepInEx version. Running it alongside BepInEx mods, such as Reroll Helper, has not been confirmed to work. Please do not simply install both loaders on top of each other.

Compatibility with other MelonLoader mods has not been comprehensively tested either. Support for additional loaders may follow later.

## License and game artwork

The independently authored mod code, build scripts and our original instructions are licensed under the [MIT License](LICENSE).

**The edited frame image is expressly excluded from that license.** It is based on the original Dungeon Settlers frame, enlarged and adapted by Danny. Rights in the original material remain with the respective rights holders. It is not a freely reusable MIT-licensed asset.

Our MIT License also does not cover game code, other game content or the optional test save.

For details, see [LICENSING.txt](LICENSING.txt), the [frame artwork notice](Assets/NOTICE.txt) and [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt). License texts for the external components used by the mod are included under [Licenses](Licenses/).

This source repository contains no game artwork, game files, mod DLL or save file. The notices also cover the contents of the separate installation packages.

Extended Hotbar is an independent community project, not an official product of CanOpener or the publisher.

## Source code and building

The mod's own source code is under [DungeonSettlers10Slots](DungeonSettlers10Slots/), with automated tests under [tests/Hotbar.Tests](tests/Hotbar.Tests/). The technical directory and DLL names are preserved for compatibility; the mod is called Extended Hotbar.

To develop or build it yourself, see [BUILDING.en.md](BUILDING.en.md). Building from source is not required for normal installation: use the ready-to-install release package once it is available.
