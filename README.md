# Extended Hotbar

**Deutsch** | [English](README.en.md)

Mehr Platz für Fähigkeiten und Gegenstände in **Dungeon Settlers** – als inoffizielle Community-Mod für MelonLoader.

## Was macht die Mod?

- Zehn Plätze für aktive Fähigkeiten. Der normale Angriff bleibt zusätzlich erhalten.
- Drei Gegenstandsplätze direkt neben der Fähigkeitsleiste.
- Fähigkeiten und Gegenstände lassen sich über die Leiste oder die zugewiesenen Tasten benutzen.
- Das gewohnte Charakterfenster mit Status, Skills und Inventar bleibt bestehen.
- Zusätzliche Tastenbelegungen sind in den Spieloptionen einstellbar. Die Bezeichnungen orientieren sich an der eingestellten Spielsprache.

## Aktueller Stand

Dieses Repository enthält den Mod-Quellcode, Tests, Anleitungen und Lizenzhinweise. Der erste Release wird vorbereitet. Nach seiner Freigabe findest du die Installationspakete im [Downloadbereich](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases).

Der aktuelle Mod-Teststand ist **0.3.6**, Paketausgabe **r2**. Diese Ausgabe aktualisiert nur Anleitungen und Lizenzhinweise; sie verändert die Mod-Funktionen nicht.

Bitte sichere deine Spielstände, bevor du eine Mod ausprobierst. Extended Hotbar ist weiterhin eine Testversion.

## Download und Installation

Wähle im Downloadbereich eines dieser beiden Pakete:

- `Extended-Hotbar-0.3.6-r2.zip`: die Mod für deine eigene Kampagne.
- `Extended-Hotbar-0.3.6-r2-mit-Testspielstand.zip`: dieselbe Mod plus optionaler Demo-Spielstand mit belegten Slots und zusätzlichen Items.

Du brauchst nur eines davon. GitHubs automatische Downloads **Source code (zip/tar.gz)** enthalten den Quellcode, nicht das fertige Installationspaket.

Installiere MelonLoader 0.7.3 separat, entpacke das gewählte Mod-ZIP und kopiere dessen Ordner `Mods` neben `DungeonSettlers.exe`. Eine ausführliche [Schritt-für-Schritt-Anleitung](DungeonSettlers10Slots/UserGuide/BITTE%20ZUERST%20LESEN.txt) liegt auch im Paket. Der optionale Spielstand wird getrennt importiert, wie in seiner beiliegenden Anleitung beschrieben.

## Voraussetzungen

- Eine legal erworbene Windows-Version von Dungeon Settlers (64 Bit).
- Unterstützter Teststand: **DS_B.0.4.17**, Steam-Build **25143510**.
- Separat installiertes **MelonLoader 0.7.3**.

Für andere Spielversionen ist die Kompatibilität nicht bestätigt. Nach einem Spielupdate kann auch die Mod ein Update benötigen.

## Standardtasten

| Funktion | Tasten |
| --- | --- |
| Fähigkeit 1 bis 10 | 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 |
| Gegenstandsplatz 1 bis 3 | Q, E, R |
| Charakter 1 bis 10 auswählen | Linke Umschalttaste + 1 bis 0 |

Bereits gespeicherte eigene Belegungen bleiben erhalten und können deshalb von dieser Tabelle abweichen. Du kannst sie in den Spieloptionen ändern. Ohne ausgewählten Charakter wird die Leiste wie im Originalspiel ausgeblendet.

## Andere Mods und Loader

Diese Ausgabe unterstützt **MelonLoader**. Eine BepInEx-Ausgabe gibt es derzeit nicht. Der gemeinsame Betrieb mit BepInEx-Mods, beispielsweise Reroll Helper, ist nicht bestätigt. Bitte nicht einfach beide Loader übereinander installieren.

Auch die Kompatibilität mit anderen MelonLoader-Mods ist nicht pauschal geprüft. Weitere Loader-Unterstützung kann später folgen.

## Lizenz und Spielgrafiken

Der eigenständig verfasste Modcode, die Build-Skripte und unsere eigenen Anleitungstexte stehen unter der [MIT-Lizenz](LICENSE).

**Die bearbeitete Rahmengrafik ist davon ausdrücklich ausgenommen.** Sie basiert auf dem Originalrahmen aus Dungeon Settlers und wurde von Danny vergrößert und angepasst. Die Rechte an den Originalbestandteilen bleiben bei den jeweiligen Rechteinhabern. Sie ist kein frei unter MIT nutzbares Asset.

Auch Spielcode, andere Spielinhalte und der optionale Testspielstand werden durch unsere MIT-Lizenz nicht freigegeben.

Die genauen Hinweise stehen in [LICENSING.txt](LICENSING.txt), im [Hinweis zur Rahmengrafik](Assets/NOTICE.txt) und in [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt). Die Lizenztexte der verwendeten externen Komponenten liegen unter [Licenses](Licenses/).

Dieses Quellcode-Repository enthält keine Spielgrafik, keine Spieldateien, keine Mod-DLL und keinen Spielstand. Die Hinweise beziehen sich auch auf die Bestandteile der getrennten Installationspakete.

Extended Hotbar ist ein unabhängiges Community-Projekt und kein offizielles Produkt von CanOpener oder dem Publisher.

## Quellcode und Selbstbau

Der eigene Modcode liegt unter [DungeonSettlers10Slots](DungeonSettlers10Slots/), die automatisierten Tests unter [tests/Hotbar.Tests](tests/Hotbar.Tests/). Der technische Ordner- und DLL-Name bleibt aus Kompatibilitätsgründen erhalten; die Mod heißt Extended Hotbar.

Wer selbst entwickeln möchte, findet die Schritte in [BUILDING.md](BUILDING.md). Zum normalen Installieren ist kein Selbstbau nötig: Verwende dafür das fertige Release-Paket, sobald es freigegeben ist.
