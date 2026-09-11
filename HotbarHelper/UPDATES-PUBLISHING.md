# Pakete veröffentlichen — ausschließlich lokaler Helper ab 0.1.13

Die frühere Online-Update-Vorbereitung wurde entfernt. Diese Datei bleibt als
Einstieg für vorhandene Dokumentationslinks erhalten, nicht als Online-Roadmap.

Die aktuelle Anleitung ist [LOCAL-UPDATES-PUBLISHING.md](LOCAL-UPDATES-PUBLISHING.md).

- Nutzer beziehen Pakete extern von ihrer gewählten Mod-Plattform. Der Helper
  fragt keine Releases ab und lädt keine Dateien herunter.
- Mod-Installation und Tastenprofile bleiben lokale, bestätigte Vorgänge.
- Für ein lokales Helper-Selbstupdate werden weiterhin die originale ZIP und
  ihre gültig signierte .zip.update.json gemeinsam in Downloads benötigt.
  Manuelles Entpacken/Starten ist davon unabhängig.
- Build/Test: Build.ps1; neues unveränderliches Paket: Package.ps1.
  Signierung ist optional und ausdrücklich freizugeben; keine privaten
  Schlüssel veröffentlichen. UpdateTrust.xml ist ausschließlich öffentlich.
- Das äußere 21-Dateien-Paketlayout und beide eingebetteten Mod-ZIPs bleiben
  erhalten. Für Änderungen an ZIPs neue Hashes/Signaturen und Tests erstellen.
- Windows-Schutz und Plattformprüfungen bleiben aktiv. Die Entfernung des
  Online-Codes ist keine Zusage einer Antivirus- oder Nexus-Freigabe.

Uploads, Commits und Veröffentlichungen bleiben manuelle Schritte des Nutzers.
