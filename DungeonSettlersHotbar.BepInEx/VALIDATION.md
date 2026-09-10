# BepInEx validation / Prüfstand — 2026-09-10

Current release candidate: **0.3.8-bepinex.1**, paired with MelonLoader **0.3.8** and Helper **0.1.12**.
Target: DS_B.0.4.19 / Steam build 25154317, Windows x64; BepInEx 6 Unity IL2CPP x64 **6.0.0-be.788+5b766a3**.

The development handover reports:
- Both loader builds completed without warnings or errors.
- 33 mod tests including the supplied save fixture passed.
- 224 Helper tests passed, including loader separation, profile conflicts, undo and signature purposes.
- 150 UI previews across ten languages were generated; selected German/Japanese views were inspected.
- BepInEx native and normal-start checks completed cleanly; the normal run recorded 548 update and three scene callbacks. Native tests included 3312 keyboard-assignment checks.
- Danny subsequently confirmed key configuration in the Steam version and other functions as successful on 2026-09-10. This does not identify every test case or establish complete BepInEx combat/long-session coverage.

The loader message `Class::Init signatures have been exhausted, using a substitute!` remains visible. Documented tests passed despite it. Unknown game fingerprints stay blocked. Long campaigns and arbitrary third-party mod combinations are not comprehensively validated.

The unchanged release ZIP has 64392 bytes and SHA256 `77193451CD045D03A50E24A80F2D0582E178F335D90926EDF9AAF9E6B3880547`. Packaged instructions retain their earlier conservative test status; no archive was repacked for this documentation update.

Deutsch: Dieser Bericht fasst den freigegebenen Entwicklungsstand und Dannys anschließende Rückmeldung zusammen. Er ist kein neuer Kompletttest und keine universelle Kompatibilitätsgarantie. Spielkopien teilen normalerweise Saves/Einstellungen; Kopien nicht gleichzeitig starten und Originalspielstände sichern.
