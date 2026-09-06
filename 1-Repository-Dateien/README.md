# Extended Hotbar

**Deutsch** | [English](README.en.md)

Mehr Platz in **Dungeon Settlers**: zehn aktive Fähigkeitsplätze, ein separater Basisangriff und drei Gegenstandsplätze. Das gewohnte Charakterfenster bleibt erhalten.

## Hier bekommst du die Mod

**[Zum Downloadbereich → Releases](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases)**

Öffne dort **Extended Hotbar 0.3.7** und scrolle ganz nach unten zu **Assets**. Falls dieser Release noch nicht sichtbar ist, ist seine Freigabe noch nicht abgeschlossen. Nimm nicht ersatzweise das ältere Paket für den neuen Spielbuild.

**Nicht über „Code → Download ZIP“ herunterladen.** Das und die automatischen **Source code (zip/tar.gz)**-Downloads sind Quellcode für Entwickler, keine installierbare Mod.

| Download | Wofür ist er? |
| --- | --- |
| **Extended-Hotbar-Helper-0.1.2-test.zip** | Optionaler Windows-Helfer zum Installieren und Aktualisieren. **Mod 0.3.7 ist schon enthalten**; kein zusätzliches Mod-ZIP nötig. |
| **Extended-Hotbar-0.3.7.zip** | Manuelle Installation für deine eigene Kampagne. |
| **Extended-Hotbar-0.3.7-mit-Testspielstand.zip** | Dasselbe manuelle Paket plus optionaler Demo-Spielstand „10 Slots Testfile“ mit belegten Slots und zusätzlichen Items. |

Die beiden manuellen Pakete enthalten dieselbe Mod: nicht beide installieren. `SHA256SUMS.txt` enthält optionale Prüfsummen aller drei Downloads. QA und Frieren gehören nicht zu diesen Paketen.

## Voraussetzungen

- Windows **64 Bit** und eine legal erworbene Kopie von Dungeon Settlers.
- Unterstützte Spielversion: **DS_B.0.4.19 / Steam-Build 25154317**.
- Separat installiertes **MelonLoader 0.7.3**. Spiel und Loader werden nicht mitgeliefert.
- Für den Helfer zusätzlich **.NET Framework 4.8**.

Dies sind **Testversionen**. Andere Spielbuilds sind nicht freigegeben. Nach einem Spielupdate kann eine neue Mod- und Helferversion nötig sein. Sichere deine Spielstände vor dem Ausprobieren.

## Installieren oder aktualisieren

### Mit dem optionalen Helfer

1. Entpacke das **gesamte Helfer-ZIP** in einen eigenen Ordner, nicht in `Mods`.
2. Beende das Spiel und alle Testkopien. Öffne `Extended-Hotbar-Helper.exe`.
3. Prüfe den angezeigten Spielordner: Dort muss `DungeonSettlers.exe` liegen.
4. Wähle **Extended Hotbar installieren / aktualisieren**, prüfe die Zusammenfassung und bestätige.
5. Starte das Spiel anschließend selbst über Steam.

Der Helfer sichert vorhandene Hotbar-Dateien vor dem Austausch. Andere Mods, MelonLoader und Spielstände bleiben bei der Installation erhalten. Er installiert MelonLoader nicht und lädt keine Updates aus dem Internet; für spätere Versionen lädst du ein neues Helfer-Paket herunter.

Bedienung und Hilfe gibt es in allen zehn Spielsprachen. Die Sprachwahl im Helfer verändert nicht deine gespeicherte Spielsprache. [Ausführliche Helfer-Anleitung](HotbarHelper/BITTE%20ZUERST%20LESEN.txt).

### Ohne Helfer: Dateien selbst kopieren

1. Beende das Spiel, sichere deine Spielstände und entpacke eines der **manuellen Mod-ZIPs**.
2. Sichere eine vorhandene `DungeonSettlers10Slots.dll` oder alte `DungeonSettlers12Slots.dll` **außerhalb** von `Mods`. Lade nicht zwei Hotbar-Versionen gleichzeitig.
3. Kopiere den enthaltenen Ordner **Mods** in den Spielordner, direkt neben `DungeonSettlers.exe`. Einen vorhandenen Mods-Ordner zusammenführen; kein `Mods/Mods` anlegen. Andere Mods nicht löschen.
4. Starte das Spiel. Die erste Einrichtung durch MelonLoader kann länger dauern; falls es sich danach schließt, starte es erneut.

Im richtigen Mod-Paket liegen `Mods/DungeonSettlers10Slots.dll` und `Mods/DungeonSettlers10SlotsAssets`. `DNST-Extended-Hotbar-main.zip` ist der falsche Download zur Installation. [Schritt-für-Schritt-Anleitung](DungeonSettlers10Slots/UserGuide/BITTE%20ZUERST%20LESEN.txt).

Den Demo-Spielstand separat nach seiner mitgelieferten Anleitung importieren, nicht in den Spielordner kopieren und keine eigene Kampagne überschreiben. Der unveränderte Demo-Spielstand wurde unter **DS_B.0.4.17** gespeichert; er ist keine neu auf 0.4.19 durchgespielte Kampagne.

## Wieder ohne Extended Hotbar spielen

Der zweite Hauptknopf des Helfers bietet an, eine **separate Spielstandskopie für die Original-Hotbar zu erstellen und Extended Hotbar zu deinstallieren**. Du wählst dafür eine Kampagne aus und bestätigst die Zusammenfassung.

Das Original bleibt erhalten. Die neue Kopie behält die ersten vier Fähigkeitsbelegungen, Basisangriff und einen Gegenstandsplatz. Gelernte Fähigkeiten und Inventar werden nicht entfernt. Die betroffenen Tasten werden auf die Originalbelegung zurückgesetzt; andere Einstellungen, Mods und MelonLoader bleiben bestehen.

**Die Spielstandsumwandlung ist experimentell.** Das vollständige Laden, Speichern und erneute Laden dieser Kopie im laufenden Spiel ist noch nicht abgenommen. Zuerst mit einer Testkampagne prüfen, nicht mit dem einzigen wichtigen Spielstand. Original und Kopie entwickeln sich getrennt; Fortschritt wird nicht zusammengeführt. Unterstützte Exportformate: 0.4.17 und 0.4.19; unbekannte Formate und Ironmode werden abgelehnt.

Im Hilfebereich liegen Rückgängig-Funktion und eine getrennte Reparatur falscher „Alpha“-Tastenanzeigen. Diese reine Tastenreparatur entfernt keine Mod und wandelt keinen Spielstand um. [Details und Sicherungen](HotbarHelper/BITTE%20ZUERST%20LESEN.txt).

## Standardtasten

| Funktion | Tasten |
| --- | --- |
| Fähigkeiten 1 bis 10 | 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 |
| Gegenstandsplätze 1 bis 3 | Q, E, R |
| Charaktere 1 bis 10 auswählen | Linke Umschalttaste + 1 bis 0 |

Klicken auf die Plätze funktioniert ebenfalls. Eigene gespeicherte Belegungen bleiben erhalten und können abweichen. Zusätzliche Tasten sind in den Spieloptionen einstellbar; ihre Namen folgen der Spielsprache. Ohne ausgewählten Charakter wird die Leiste wie im Original ausgeblendet.

## Was ist neu und was wurde geprüft?

**Mod 0.3.7:** Anpassung an DS_B.0.4.19 / Build 25154317. Slot-Funktionen, Rahmengrafik und Demo-Spielstand bleiben gegenüber 0.3.6 unverändert.

**Helfer 0.1.2:** Enthält Mod 0.3.7, zwei klare Hauptaktionen, getrennte Auswahl und Bestätigung beim Entfernen, aufgeräumte Hilfe und Texte in zehn Sprachen. Ein Fehler beim wiederholten Sprachwechsel wurde behoben. Installationen ohne tatsächliche Änderungen erzeugen keine irreführenden Rückgängig-Einträge.

Automatisierte Tests und Spiel-Schnittstellentests ohne sichtbare Oberfläche haben unter anderem Slots, Speichern/Laden, Tastenbelegung und zehn Sprachen geprüft. Das ersetzt keinen vollständigen sichtbaren Kampagnen-, Kampf- oder Langzeittest im neuen Build. Die experimentelle Helfer-Spielstandskopie ist dadurch ausdrücklich nicht als fertig validiert erfasst. Übersetzungsfeedback ist willkommen.

## Andere Mods und Loader

Nur **MelonLoader**. Es gibt noch keine BepInEx-Ausgabe; gemeinsamer Betrieb mit BepInEx-Mods wie Reroll Helper ist nicht bestätigt. Installiere nicht einfach beide Loader übereinander. Auch andere MelonLoader-Mods sind nicht pauschal kompatibilitätsgeprüft.

## Lizenz und Spielgrafik

Eigenständig verfasster Mod- und Helfercode, Build-Skripte und eigene Anleitungen stehen unter [MIT](LICENSE).

**Die bearbeitete Original-Rahmengrafik ist nicht MIT-lizenziert.** Sie basiert auf Dungeon Settlers und wurde von Danny vergrößert und angepasst; Rechte am Original bleiben bei den jeweiligen Rechteinhabern. Spielcode, Spielinhalte und der optionale Demo-Spielstand sind ebenfalls nicht von unserer Code-Lizenz erfasst. Das gilt auch für die im Helfer eingebettete Grafik.

Siehe [LICENSING.txt](LICENSING.txt), [Grafikhinweis](Assets/NOTICE.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) und [Lizenztexte](Licenses/). Die Hinweise bleiben in allen Downloadpaketen enthalten.

Dieses Quellcode-Repository enthält keine Spielgrafik, Spiel-/Loader-DLL, fertige Mod-DLL oder echte Kampagnendatei. `HotbarHelper/SyntheticSave.json` ist eine selbst verfasste Testdatei, kein Spielstand zum Spielen.

Inoffizielles Community-Projekt, kein offizielles Produkt von CanOpener oder dem Publisher.

## Für Entwickler

[Modcode](DungeonSettlers10Slots/) · [Helfercode](HotbarHelper/) · [Modtests](tests/Hotbar.Tests/) · [Bauanleitung](BUILDING.md). Die technischen Ordner- und DLL-Namen bleiben aus Kompatibilitätsgründen bestehen. Zum Spielen musst du nichts selbst kompilieren.
