# Extended Hotbar & Hotbar Helper

**Deutsch** | [English](README.en.md)

Zehn aktive Fähigkeitsplätze, ein separater Basisangriff und drei Gegenstandsplätze für Dungeon Settlers. Der optionale Helper erleichtert Installation, lokale Updates und Tastenbelegung.

**Vorbereitete Vorabversion: Helper 0.1.13 · MelonLoader-Mod 0.3.8 · BepInEx-Mod 0.3.8-bepinex.1**

## Downloads

Öffne [Releases](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases) und prüfe die angebotenen Dateien unter **Assets**. Helper 0.1.13 ist zur Veröffentlichung vorbereitet; bis sein Paket dort angeboten wird, sind nur frühere Helper-Versionen verfügbar. Die beiden Mod-Pakete bleiben unverändert. „Source code“ und „Code → Download ZIP“ sind keine Installationspakete.

| Verwendung | Datei |
| --- | --- |
| Vorbereitetes Helper-Paket; beide Mod-Ausgaben enthalten | **Extended-Hotbar-Helper-0.1.13-test.zip** |
| Manuelle Installation für MelonLoader | **Extended-Hotbar-0.3.8.zip** |
| Manuelle Installation für BepInEx | **Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip** |
| Optionale Prüfsummen | **SHA256SUMS.txt** |

Wähle die Helper-ZIP oder die zu deinem Loader passende Mod-ZIP. Nur das MelonLoader-Mod-Paket enthält den optionalen Demo-Spielstand DS_B.0.4.17. Der Helper kann das ausgewählte eingebettete Paket exportieren. Kein Spielstand wird automatisch importiert.

## Voraussetzungen und Start

- Windows x64; Dungeon Settlers **DS_B.0.4.19 / Steam-Build 25154317**.
- Separat installiertes **MelonLoader 0.7.3** oder **BepInEx 6 Unity IL2CPP x64**, getestet mit **6.0.0-be.788+5b766a3**. BepInEx 5 und Mono werden nicht unterstützt. Loader nicht in derselben Spielkopie mischen.
- Der Helper benötigt **.NET Framework 4.8**. Spiel und Loader werden nicht mitgeliefert oder vom Helper installiert.

1. Spielstände sichern und alle Spielinstanzen schließen. Spielkopien teilen normalerweise dieselben Saves und Einstellungen; eine Kopie ist kein Save-Backup.
2. Die ganze Helper-ZIP in einen eigenen Ordner entpacken und `Extended-Hotbar-Helper.exe` öffnen.
3. Richtigen Spielordner und Loader prüfen, dann die Mod-Installation bestätigen. Fehlende, falsche oder gemischte Loader werden abgewiesen.
4. Danach das Spiel selbst starten. Andere Mods bleiben unangetastet.

Manuell: Beim MelonLoader-Paket den enthaltenen `Mods`-Ordner mit dem Spielordner zusammenführen; beim BepInEx-Paket den `BepInEx`-Ordner. DLL und mitgelieferter Grafikordner gehören zusammen. Vor einem Update alte Hotbar-Dateien außerhalb des Mod-Ordners sichern. Details in den Paket-Anleitungen.

## Tasten selbst wählen

Die Installation verändert keine Belegungen. Native Standardtasten bleiben erhalten; zusätzliche Skill- und Item-Aktionen starten in beiden Spalten unbelegt. Bereits gespeicherte Belegungen und alte Konflikte werden nicht ungefragt geändert.

Belege die Tasten im Spiel selbst oder wende ausdrücklich das optionale Helper-Profil an: Skills **1–0**, Charaktere **linke Umschalttaste + 1–0**, Items **Q/E/R**, additive Auswahl unbelegt. Das Profil ersetzt beide Spalten dieser Aktionen. Weitere belegte Aktionen werden nur nach zusätzlicher Konfliktbestätigung geändert. Vorschau, Sicherung und Rücknahme sind vorhanden; reine Charakterprofile bleiben verfügbar.

## Lokale Updates

Helper 0.1.13 arbeitet ausschließlich lokal. HTTP-Client, Release-Feed und Online-Updatefunktionen sind entfernt. Lade Pakete selbst über die gewählte Mod-Plattform herunter. Die optionale Suche nach bereits vorhandenen Paketen im Windows-Downloadordner bleibt verfügbar.

Ein lokales Selbstupdate benötigt die neue Helper-ZIP und eine gültige, exakt passende signierte `.zip.update.json` im selben Windows-Downloadordner. Für das vorbereitete 0.1.13-Paket wurde noch keine solche Signaturdatei erzeugt. Entpacke deshalb die gesamte ZIP manuell in einen eigenen Ordner, sobald sie verfügbar ist, und starte den Helper dort. Eine Signaturdatei eines älteren Pakets ist kein Ersatz.

„Nur Helper aktualisieren“ installiert keine Mod. Bei einem gültig signierten lokalen Selbstupdate liegt die neue EXE unter `%LOCALAPPDATA%\ExtendedHotbarHelper\Updates`; alte EXE und Verknüpfungen bleiben erhalten. Die BepInEx-Unterstützung ist seit Helper 0.1.12 enthalten.

Prüfsummen ersetzen keine Signatur. Die Paketsignatur ist keine Windows-Authenticode-Signatur; eine Windows-Startwarnung kann weiterhin erscheinen.

## Prüfstand und Grenzen

Der vollständige Helper-0.1.13-Quellstand wurde am 11.09.2026 lokal neu gebaut und paketiert: 221 Tests bestanden, 0 fehlgeschlagen. Für die Paketvorbereitung vom 10.09.2026 sind außerdem 150 UI-Vorschauen in zehn Sprachen dokumentiert. Die Tests prüfen unter anderem lokale Paketwahl, Signaturen, Einstellungen und die Entfernung der bisherigen Netzwerkkomponenten. Das ist keine Freigabe durch Antivirus-Scanner oder Mod-Plattformen und kein Ergebnis eines neuen GitHub-Prüflaufs für 0.1.13.

Für die unveränderten Mod-Pakete sind zuvor 33 Mod-Tests mit dem bereitgestellten Save-Testfall und beide Loader-Builds ohne Warnungen/Fehler dokumentiert. Dannys erfolgreicher Steam-Test der Tastenbelegung und weiterer Funktionen am 10.09.2026 galt dem vorherigen Helper 0.1.12; ein neuer Spieltest von 0.1.13 wurde nicht durchgeführt. Das belegt keine vollständige Langzeitkampagnen- oder Fremdmod-Kompatibilität. Die unveränderten ZIP-Anleitungen dokumentieren teilweise noch den früheren, konservativeren Teststand.

Die BepInEx-Meldung `Class::Init signatures have been exhausted, using a substitute!` bleibt sichtbar; die dokumentierten Tests bestehen trotzdem. Unbekannte Spielbuilds bleiben blockiert.

Die experimentelle Rückkehr zu Vanilla erzeugt einen zusätzlichen manuellen Save **in derselben Kampagne** und entfernt Extended Hotbar. Das Original wird bewahrt und gesichert; Autosaves sind gemeinsam. Andere Mods und der Loader bleiben installiert.

[Helper-Anleitung](HotbarHelper/BITTE%20ZUERST%20LESEN.txt) · [MelonLoader-Anleitung](DungeonSettlers10Slots/UserGuide/BITTE%20ZUERST%20LESEN.txt) · [BepInEx-Anleitung](DungeonSettlersHotbar.BepInEx/README.md) · [Selbst bauen](BUILDING.md)

Inoffizielles Community-Projekt, kein offizielles Produkt von CanOpener oder dem Publisher. Eigener Code und eigene Anleitungen: MIT; Spielgrafik, Demo und Spielinhalte sind ausgeschlossen. Siehe [LICENSE](LICENSE), [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt) und [Grafikhinweis](Assets/NOTICE.txt).
