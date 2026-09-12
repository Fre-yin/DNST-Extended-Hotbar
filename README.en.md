# Extended Hotbar

[Deutsch](README.md) | **English**

Ten active skill slots, a separate basic attack and three item slots for Dungeon Settlers.

**Preview versions: MelonLoader 0.3.9 · BepInEx 0.3.9-bepinex.1**

## Downloads

Download the standalone mod package matching your installed loader:

| Loader | Mod package |
| --- | --- |
| MelonLoader | [Extended-Hotbar-0.3.9.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v0.3.9/Extended-Hotbar-0.3.9.zip) |
| BepInEx | [Extended-Hotbar-BepInEx-0.3.9-bepinex.1.zip](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases/download/v0.3.9/Extended-Hotbar-BepInEx-0.3.9-bepinex.1.zip) |

These links go directly to the respective mod ZIPs. Automatically generated source-code archives are not installation packages.

Only the MelonLoader package includes the optional DS_B.0.4.17 demo save. It is not imported automatically and is not required to use the mod.

## Requirements and installation

- Windows x64 and Dungeon Settlers **DS_B.0.4.23 / Steam build 25269660**.
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

Both mod editions remain previews for the game build listed above. Both final release builds completed with 0 warnings and 0 errors. Validation documents 33 data tests with the unchanged demo and 32 without it, plus native checks on both loaders for slots, context menus, bindings, temporary save/load, ten languages and frame lifetime.

A subsequent visible BepInEx user check was confirmed successful. Individual combat, click and save actions were not recorded. Complete combat, campaign, long-session and third-party-mod compatibility is therefore not confirmed. Unknown game builds remain blocked.

The tested BepInEx loader reports `Class::Init signatures have been exhausted, using a substitute!`; the documented tests pass despite this message.

Removing the mod does not automatically convert modded saves to vanilla saves. Keep your backups.

## Project focus

Future work focuses on the two standalone Hotbar mods. Follow this page for installation and key bindings.

[Building](BUILDING.en.md)

Unofficial community project, not an official CanOpener or publisher product. Original code and instructions: MIT; game artwork, demo saves and game content are excluded. See [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) and [artwork notice](Assets/NOTICE.txt).
