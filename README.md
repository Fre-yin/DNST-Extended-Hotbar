# Extended Hotbar & Hotbar Helper

**Deutsch** | [English](README.en.md)

Extended Hotbar ergänzt Dungeon Settlers um zehn aktive Fähigkeitsplätze, einen separaten Basisangriff und drei Gegenstandsplätze. Der optionale Windows-Helper installiert und aktualisiert die Mod.

**Stand: Mod 0.3.7 · Helper 0.1.11 (Vorabversion)**

## Downloads

Fertige Pakete findest du unter [Releases](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases), beim Vorabrelease **Helper 0.1.11** unter **Assets**. „Code → Download ZIP“ und „Source code“ sind nur Quellcode.

| Du möchtest … | Download |
| --- | --- |
| Die Mod mit dem Helper installieren | **Extended-Hotbar-Helper-0.1.11-test.zip** |
| Die Mod ohne Helper manuell installieren | **Extended-Hotbar-0.3.7.zip** |

Wähle ein Paket. Der Helper enthält die Mod bereits. Beide Pakete enthalten denselben optionalen Testspielstand; er wird **nicht automatisch importiert**. Im Helper lässt sich das enthaltene Mod-Paket im Hilfebereich speichern.

## Mit dem Helper starten

Benötigt: **Windows x64**, **.NET Framework 4.8**, **Dungeon Settlers DS_B.0.4.19 / Steam 25154317** und separat installiertes **MelonLoader 0.7.3**.

1. Spielstände sichern und das Spiel samt Testkopien schließen.
2. Das ganze Helper-ZIP in einen eigenen Ordner entpacken, nicht in Mods.
3. `Extended-Hotbar-Helper.exe` öffnen. Bei mehreren gefundenen Installationen den richtigen Pfad aus der Liste wählen. **Suchen** wiederholt die Suche; **Manuell auswählen…** bleibt als Alternative.
4. **Installieren / aktualisieren… → Mod installieren / aktualisieren** wählen, Ziel prüfen und bestätigen. Danach das Spiel selbst starten.

Der Helper verändert bei der Mod-Installation keine fremden Mods, den Loader oder Spielstände. **Charaktertasten einrichten…** bietet 1–0 oder Umschalt+1–0. Konflikte werden angezeigt; Überschreiben erfordert eine ausdrückliche Bestätigung.

## Lokale Updates

Neue Pakete selbst in den Windows-Downloadordner laden. Online-Updates sind für später geplant und derzeit ausgeschaltet.

Für **Nur Helper aktualisieren** müssen das Helper-ZIP und seine passende **.zip.update.json** gemeinsam in Downloads liegen. Die Prüfdatei wird separat unter Assets angeboten. Die neue Helper-Version öffnet sich ohne Mod-Installation; die alte EXE und Verknüpfungen bleiben erhalten.

Eine Mod-ZIP wird ebenfalls lokal erkannt und vor der Installation geprüft. Unbekannte Spielbuilds bleiben gesperrt. **SHA256SUMS.txt** enthält optionale Prüfsummen; sie ersetzt die signierte Helper-Prüfdatei nicht.

## Hinweise

- „Spielstand auf Vanilla zurücksetzen“ erstellt einen zusätzlichen manuellen Spielstand in derselben Kampagne und deinstalliert Extended Hotbar. Das Original wird erhalten und gesichert; die Kampagnen-Autosaves sind gemeinsam. Original und Sicherung behalten.
- Der mitgelieferte Testspielstand ist der unveränderte Snapshot aus DS_B.0.4.17. Import nur nach seiner beiliegenden Anleitung.
- Vorabversion: automatisierte Tests ersetzen keine vollständigen Spieltests. Andere Mods und künftige Spielpatches sind nicht pauschal unterstützt.
- Die Paket-Signatur ist keine Windows-Herausgebersignatur. Eine Windows-Startwarnung kann weiterhin erscheinen.

[Helfer-Anleitung](HotbarHelper/BITTE%20ZUERST%20LESEN.txt) · [Manuelle Installation](DungeonSettlers10Slots/UserGuide/BITTE%20ZUERST%20LESEN.txt) · [Selbst bauen](BUILDING.md)

Inoffizielles Community-Projekt, kein offizielles Produkt von CanOpener oder dem Publisher. Spiel und Loader werden nicht mitgeliefert. Eigener Code und eigene Anleitungen: MIT; Spielgrafik und Spielinhalte sind davon ausgeschlossen. Siehe [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) und [Grafikhinweis](Assets/NOTICE.txt).
