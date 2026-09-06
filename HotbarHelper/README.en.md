# Extended Hotbar Helper

[Deutsch](README.md) | **English** | [Back to Extended Hotbar](../README.en.md)

**Install, update or return to the original hotbar without searching for DLL files yourself.**

Hotbar Helper is an optional Windows application for **Extended Hotbar**. Select your game folder, choose an action and confirm the summary. **Mod 0.3.7 is already included.** You do not need a separate mod ZIP.

## What does the helper do?

- **Install and update:** Install the bundled hotbar version into the selected game folder. Existing hotbar files are backed up first; the legacy 12-slot DLL is removed so two versions do not load together.
- **Return to the original hotbar:** Prepare a separate copy of your selected campaign and remove Extended Hotbar. Your original is preserved. **This save conversion is still experimental.**
- **Undo changes:** Restore backed-up mod files or affected key bindings for a selected change. This does not roll back your campaign progress.
- **Repair incorrect “Alpha” labels:** Restore the affected original key bindings without converting a save.
- **Show loading status:** While open, check whether the hotbar activated during the current game launch. After a confirmed loading failure, offer key repair once the game closes – only with your confirmation.

Other mods and MelonLoader remain installed. Installation does not modify your saves. The helper does not start or close the game.

## Download and first launch

**[Open downloads](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases)** → **Extended Hotbar 0.3.7** release → choose one of these packages under **Assets** at the bottom:

- **Extended-Hotbar-Helper-0.1.2-test.zip:** helper with the bundled mod, without a demo save.
- **Extended-Hotbar-Helper-0.1.2-test-mit-Testspielstand.zip:** the same helper and mod, plus the optional demo save.

No additional mod package needed. Import the demo separately using the instructions in the included **Testspielstand** folder; the helper does not import it automatically. If the new release is missing, its publication has not been completed.

**Do not use “Code → Download ZIP” or “Source code”.** Those are developer files, not the ready-to-use helper.

1. Back up your saves and close the game and all test copies.
2. Extract the **entire helper ZIP** into its own folder, for example on your desktop. Not into Mods.
3. Open **Extended-Hotbar-Helper.exe**.
4. Check the game folder: it must contain **DungeonSettlers.exe**.
5. Choose **Install / update Extended Hotbar**, read the summary and confirm.
6. Start the game yourself through Steam afterwards.

Requires **64-bit Windows**, **.NET Framework 4.8**, **Dungeon Settlers DS_B.0.4.19 / Steam build 25154317** and separately installed **MelonLoader 0.7.3**. The helper does not install MelonLoader.

## Play without the mod again

The second main button takes you to campaign selection. After your explicit confirmation, it creates a **separate save copy**, removes Extended Hotbar and restores the affected original key bindings.

The copy keeps the first **four skill assignments**, basic attack and **one item slot**. Learned skills and inventory are not removed. Your original stays unchanged and is also backed up. Other settings and mods remain – this does not automatically make the whole game unmodded.

**Important: This conversion is experimental.** A full in-game load/save/reload test of the converted copy remains outstanding. Try a test campaign first, not your only important save. Supported export formats: **0.4.17 and 0.4.19**; Ironmode and unknown formats are refused.

Continue with the new copy when playing without the hotbar. Original and copy progress independently; their progress is never merged.

## Languages, backups and updates

Controls and help support **German, English, Korean, French, Russian, Simplified and Traditional Chinese, Japanese, Spanish and Brazilian Portuguese**. Selecting a helper language does not change your saved game language. Windows file dialogs follow the Windows language.

Backups are stored under `%LOCALAPPDATA%\ExtendedHotbarHelper\Backups`. Keep them while you need undo. Files or affected bindings that have changed since the operation are not blindly overwritten. See the [complete instructions](READ%20FIRST%20-%20ENGLISH.txt).

The helper works **offline** and does not automatically check for new versions. “Update” means installing **the mod version bundled with this helper**. Download a new matching helper package for later game or mod versions. Unconfirmed game builds are refused.

The helper is an **unsigned test version**. If Windows displays a warning, only use a trusted package; do not disable security software. It does not require administrator privileges.

## Rights and source code

Independently authored helper code and original instructions: [MIT](LICENSE). **The edited original game artwork bundled inside the helper is excluded.** Rights in the game and its content remain with their holders. The game and MelonLoader are not bundled.

[License scope](../LICENSING.txt) · [Artwork notice](../Assets/NOTICE.txt) · [Third-party notices](../THIRD_PARTY_NOTICES.txt).

Unofficial community tool, not an official product of CanOpener or the publisher.

**For developers:** [Building, tests and technical limitations](DEVELOPMENT.md). You do not need to code to use the helper.
