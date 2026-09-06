# Extended Hotbar Helper

**Deutsch** | [English](README.en.md) | [Zur Extended Hotbar](../README.md)

**Die Mod installieren, aktualisieren oder wieder zur Original-Hotbar zurückkehren – ohne selbst DLL-Dateien zusammensuchen zu müssen.**

Der Hotbar Helper ist ein optionales Windows-Programm für **Extended Hotbar**. Du wählst deinen Spielordner, klickst auf die gewünschte Aktion und bestätigst die Zusammenfassung. **Die Mod 0.3.7 ist bereits enthalten.** Du brauchst kein zusätzliches Mod-ZIP.

## Was kann der Helfer?

- **Installieren und aktualisieren:** Die enthaltene Hotbar-Version in den ausgewählten Spielordner einsetzen. Vorhandene Hotbar-Dateien werden vorher gesichert; eine alte 12-Slot-DLL wird entfernt, damit nicht zwei Versionen gleichzeitig laden.
- **Zur Original-Hotbar zurückkehren:** Eine separate Kopie deiner ausgewählten Kampagne vorbereiten und Extended Hotbar entfernen. Dein Original bleibt erhalten. **Diese Spielstandsumwandlung ist noch experimentell.**
- **Änderungen rückgängig machen:** Gesicherte Moddateien oder betroffene Tasten einer ausgewählten Änderung wiederherstellen. Das ist kein Zurücksetzen deines Kampagnenfortschritts.
- **Falsche „Alpha“-Beschriftungen reparieren:** Die betroffenen Tasten auf die Originalbelegung zurücksetzen, ohne einen Spielstand umzuwandeln.
- **Ladestatus anzeigen:** Solange der Helfer geöffnet ist, prüfen, ob die Hotbar beim aktuellen Spielstart aktiviert wurde. Nach einem bestätigten Ladefehler bietet er nach Spielende eine Tastenreparatur an – nur mit deiner Bestätigung.

Andere Mods und MelonLoader bleiben installiert. Bei der Installation werden deine Spielstände nicht verändert. Der Helfer startet oder beendet das Spiel nicht.

## Download und erster Start

**[Downloadbereich öffnen](https://github.com/Fre-yin/DNST-Extended-Hotbar/releases)** → Release **Extended Hotbar 0.3.7** → unten bei **Assets** eines dieser Pakete wählen:

- **Extended-Hotbar-Helper-0.1.2-test.zip:** Helper mit enthaltener Mod, ohne Demo-Spielstand.
- **Extended-Hotbar-Helper-0.1.2-test-mit-Testspielstand.zip:** derselbe Helper mit derselben Mod, zusätzlich der optionale Demo-Spielstand.

Kein weiteres Mod-Paket nötig. Den Demo-Spielstand separat nach der Anleitung im enthaltenen Ordner **Testspielstand** importieren; der Helper erledigt das nicht automatisch. Falls der neue Release noch fehlt, ist seine Freigabe noch nicht abgeschlossen.

**Nicht „Code → Download ZIP“ oder „Source code“ verwenden.** Das sind Entwicklerdateien, kein fertiger Helfer.

1. Sichere deine Spielstände und schließe das Spiel sowie alle Testkopien.
2. Entpacke das **gesamte Helfer-ZIP** in einen eigenen Ordner, beispielsweise auf deinem Desktop. Nicht in den Mods-Ordner.
3. Öffne **Extended-Hotbar-Helper.exe**.
4. Prüfe den Spielordner: Dort muss **DungeonSettlers.exe** liegen.
5. Wähle **Extended Hotbar installieren / aktualisieren**, lies die Zusammenfassung und bestätige.
6. Starte das Spiel danach selbst über Steam.

Du benötigst **Windows 64 Bit**, **.NET Framework 4.8**, **Dungeon Settlers DS_B.0.4.19 / Steam-Build 25154317** und separat installiertes **MelonLoader 0.7.3**. Der Helfer installiert MelonLoader nicht.

## Wieder ohne die Mod spielen

Der zweite Hauptknopf führt dich zur Auswahl einer Kampagne. Nach deiner ausdrücklichen Bestätigung erstellt er eine **separate Spielstandskopie**, entfernt Extended Hotbar und richtet die zugehörigen Originaltasten ein.

Die Kopie behält die ersten **vier Fähigkeitsbelegungen**, den Basisangriff und **einen Gegenstandsplatz**. Gelernte Fähigkeiten und Inventar werden nicht entfernt. Das Original bleibt unverändert und wird zusätzlich gesichert. Andere Einstellungen und Mods bleiben bestehen – das Spiel ist dadurch nicht automatisch vollständig ungemoddet.

**Wichtig: Diese Umwandlung ist eine Testfunktion.** Ein vollständiger Laden-/Speichern-/Neuladen-Test der Kopie im Spiel steht noch aus. Probiere sie zuerst mit einer Testkampagne aus, nicht mit deinem einzigen wichtigen Spielstand. Unterstützte Exportformate: **0.4.17 und 0.4.19**; Ironmode und unbekannte Formate werden abgelehnt.

Spiele ohne Hotbar anschließend die neue Kopie weiter. Original und Kopie entwickeln sich getrennt; ihr Fortschritt wird nicht zusammengeführt.

## Sprachen, Sicherungen und Updates

Die Oberfläche und Hilfe unterstützen **Deutsch, Englisch, Koreanisch, Französisch, Russisch, vereinfachtes und traditionelles Chinesisch, Japanisch, Spanisch und brasilianisches Portugiesisch**. Die Sprachwahl im Helfer verändert nicht deine gespeicherte Spielsprache. Windows-Dateidialoge folgen der Windows-Sprache.

Sicherungen liegen unter `%LOCALAPPDATA%\ExtendedHotbarHelper\Backups`. Bewahre sie auf, solange du Änderungen rückgängig machen möchtest. Inzwischen veränderte Dateien oder betroffene Tasten werden nicht blind überschrieben. Einzelheiten stehen in der [vollständigen Anleitung](BITTE%20ZUERST%20LESEN.txt).

Der Helfer arbeitet **offline** und sucht nicht automatisch nach neuen Versionen. „Aktualisieren“ bedeutet: die **in diesem Helfer enthaltene Modversion** einsetzen. Für spätere Spiel- oder Modversionen lädst du ein neues passendes Helfer-Paket herunter. Nicht bestätigte Spielbuilds werden abgelehnt.

Der Helfer ist eine **unsignierte Testversion**. Bei einer Windows-Warnung nur ein vertrauenswürdiges Paket verwenden; Sicherheitssoftware nicht abschalten. Er benötigt keine Administratorrechte.

## Rechte und Quellcode

Eigenständig verfasster Helfercode und eigene Anleitungen: [MIT](LICENSE). **Die im Helfer enthaltene bearbeitete Original-Spielgrafik ist davon ausgenommen.** Rechte am Spiel und seinen Inhalten bleiben bei den jeweiligen Rechteinhabern. Spiel und MelonLoader werden nicht mitgeliefert.

[Rechtehinweise](../LICENSING.txt) · [Grafikhinweis](../Assets/NOTICE.txt) · [Fremdkomponenten](../THIRD_PARTY_NOTICES.txt).

Inoffizielles Community-Werkzeug, kein offizielles Produkt von CanOpener oder dem Publisher.

**Für Entwickler:** [Bauen, Tests und technische Grenzen](DEVELOPMENT.md). Zum Benutzen musst du nicht programmieren.
