# Extended Hotbar

[Deutsch](README.md) | **English**

Ten active skill slots, a separate basic attack and three item slots for Dungeon Settlers.

**Version 1.0.0 for MelonLoader and BepInEx — feature-complete release.**

## Downloads

Download the standalone mod package matching your installed loader:

| Loader | Mod package |
| --- | --- |
| MelonLoader | [Extended-Hotbar-1.0.0.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v1.0.0/Extended-Hotbar-1.0.0.zip) |
| BepInEx | [Extended-Hotbar-BepInEx-1.0.0.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v1.0.0/Extended-Hotbar-BepInEx-1.0.0.zip) |

These links go directly to the respective mod ZIPs. Automatically generated source-code archives are not installation packages.

Only the MelonLoader package includes the optional DS_B.0.4.17 demo save. It is not imported automatically and is not required to use the mod.

## Requirements and installation

- Windows x64 and Dungeon Settlers **Steam build 25284551 on the unchanged DS_B.0.4.23 code baseline**. Build 25269660 uses the same verified game-code and metadata fingerprints.
- Separately installed **MelonLoader 0.7.3** or **BepInEx 6 Unity IL2CPP x64**, tested with **6.0.0-be.788+5b766a3**. BepInEx 5 and Mono are unsupported.
- Use one loader per game folder. The packages include neither the game nor a loader.

1. Back up saves and close all game instances. Separate game copies may share saves and settings.
2. Extract the mod ZIP matching your loader.
3. For MelonLoader, merge the included **Mods** folder into your game folder. For BepInEx, merge the included **BepInEx** folder into your game folder.
4. Keep the supplied folder structure: the mod DLL and artwork folder belong together. Before updating, back up the previous Hotbar files outside the mod folder.
5. Start the game, check the mod in a test campaign first, and assign your preferred keys in the game options.

## Choose your own keys

Native defaults and saved bindings are preserved. Additional skill and item actions without a saved binding start unbound in both columns.

Choose your own bindings in the game options. There is no required key profile. Existing binding conflicts are not automatically cleared; adjust the affected actions individually if needed.

## Validation and limitations

Both final 1.0.0 builds completed with 0 warnings and 0 errors. Package tests passed 33/33 for MelonLoader and 32/32 for BepInEx. Both final DLLs passed the full native checks on Steam build 25284551 for slots, context menus, bindings, temporary save/load, ten languages and frame lifetime. BepInEx exited with code 0 and clean runtime cleanup; MelonLoader was deliberately stopped after its successful checks within the planned 45-second test window.

Version 1.0.0 marks the completed feature set. These checks do not fully establish visible combat, long sessions, campaign switching, every resolution or arbitrary third-party-mod combinations. The test runs changed no real campaign, personal bindings or Steam files. Different game-code or metadata fingerprints remain blocked.

Hotbar, save, binding, input, layout and asset logic are unchanged from 0.3.9. The game patch changed only Addressables, including UI prefabs and tables. Version information, compatibility messages and documentation were updated. See the [changelog](DungeonSettlers10Slots/CHANGELOG.md) and [patch report](DungeonSettlers10Slots/PATCH-25284551.md).

The tested BepInEx loader reports `Class::Init signatures have been exhausted, using a substitute!`; the documented tests pass despite this message.

Removing the mod does not automatically convert modded saves to vanilla saves. Keep your backups.

## Project focus

Future work focuses on the two standalone Hotbar mods. Follow this page for installation and key bindings.

[Building](BUILDING.en.md)

Unofficial community project, not an official CanOpener or publisher product. Original code and instructions: MIT; game artwork, demo saves and game content are excluded. See [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) and [artwork notice](Assets/NOTICE.txt).
