# Extended Hotbar & Hotbar Helper

[Deutsch](README.md) | **English**

Extended Hotbar adds ten active skill slots, a separate basic attack and three item slots to Dungeon Settlers. The optional Windows helper installs and updates the mod.

**Current package: mod 0.3.7 · helper 0.1.11 (prerelease)**

## Downloads

Open [Releases](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases) and find **Helper 0.1.11 → Assets**. “Code → Download ZIP” and “Source code” contain source, not an installable mod.

| You want to … | Download |
| --- | --- |
| Install the mod with the helper | **Extended-Hotbar-Helper-0.1.11-test.zip** |
| Install the mod manually without the helper | **Extended-Hotbar-0.3.7.zip** |

Choose one package. The helper already contains the mod. Both packages include the same optional demo save; it is **never imported automatically**. The helper can save its embedded mod package from the troubleshooting area.

## Start with the helper

Requires **Windows x64**, **.NET Framework 4.8**, **Dungeon Settlers DS_B.0.4.19 / Steam 25154317** and separately installed **MelonLoader 0.7.3**.

1. Back up your saves and close the game and all test copies.
2. Extract the entire helper ZIP into its own folder, not Mods.
3. Open `Extended-Hotbar-Helper.exe`. If several installations are found, select the correct full path. **Search** repeats detection; **Select manually…** remains available.
4. Choose **Install / update… → Install / update mod**, check the destination and confirm. Start the game yourself afterwards.

Mod installation preserves unrelated mods, the loader and saves. **Set up character keys…** offers 1–0 or Shift+1–0. Conflicts are shown; overwriting requires explicit confirmation.

## Local updates

Download new packages yourself into Windows Downloads. Online updates are planned for later and currently disabled.

For **Update helper only**, place the helper ZIP and its matching **.zip.update.json** together in Downloads. The verification file is a separate release asset. The new helper opens without installing the mod; the old EXE and shortcuts remain.

Mod ZIPs are also detected locally and checked before installation. Unknown game builds remain blocked. **SHA256SUMS.txt** provides optional checksums; it does not replace the signed helper verification file.

## Notes

- Returning a save to vanilla creates an additional manual save in the same campaign and uninstalls Extended Hotbar. The original is preserved and backed up; campaign autosaves are shared. Keep the original and backup.
- The bundled demo is the unchanged DS_B.0.4.17 snapshot. Import it only using its included instructions.
- Prerelease: automated checks do not replace complete playtesting. Compatibility with other mods or future game patches is not generally guaranteed.
- The package signature is not Windows Authenticode. Windows may still display a publisher warning.

[Helper instructions](HotbarHelper/READ%20FIRST%20-%20ENGLISH.txt) · [Manual installation](DungeonSettlers10Slots/UserGuide/READ%20FIRST%20-%20ENGLISH.txt) · [Build from source](BUILDING.en.md)

Unofficial community project, not an official CanOpener or publisher product. Game and loader are not bundled. Original code and instructions: MIT; game artwork and content are excluded. See [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) and the [artwork notice](Assets/NOTICE.txt).
