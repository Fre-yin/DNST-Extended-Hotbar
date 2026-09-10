# Updates veröffentlichen — Helper 0.1.5 und später

**Aktueller Stand 0.1.12: Online-Updates sind fest deaktiviert.** Diese Anleitung
beschreibt die vorbereitete spätere Online-Funktion, nicht die aktive Oberfläche.
Aktuell gilt [LOCAL-UPDATES-PUBLISHING.md](LOCAL-UPDATES-PUBLISHING.md).
`Package.ps1` signiert Online-Pakete nur mit dem expliziten Schalter
`-SignUpdatePackage`. Derselbe Signaturtyp wird bereits für lokale Helper-Updates
verwendet, ohne Netzwerk. Hardware-Schlüssel, Wiederherstellung und Live-Test sind vertagt.

Der Helper lädt fertige Pakete aus dem fest eingestellten Repository
`Fre-yin/DNST-Extended-Hotbar`. Er lädt weder den Quellcode noch den Ordner auf
der Repository-Startseite. Ein Git-Tag allein ist kein Update.

## Einmalig für den Einstieg

Nutzer mit Helper 0.1.4 oder älter müssen Helper 0.1.5 einmal selbst herunterladen.
Die alten Programme haben noch keinen Update-Check. Erst ab 0.1.5 können weitere
Helper-Pakete direkt angeboten und geladen werden.

Repository und Release müssen öffentlich erreichbar sein. Es wird kein persönlicher
GitHub-Zugang in den Helper eingebaut. Diese Aufgabe hat nichts veröffentlicht und
die Sichtbarkeit nicht verändert. Prüfe vor einer Veröffentlichung, dass keine
privaten Daten oder ungewollten Dateien in Repository und Historie liegen.

## Jedes spätere Update

1. Spielpatch/Modänderungen zuerst prüfen und den Helper samt mitgelieferter Mod
   aktualisieren. Unterstützte Spielfingerprints nicht einfach freigeben.
2. Eine neue, höhere Helper-Version vergeben, auch bei einem reinen Modupdate.
   Versionen in Updates.cs, MainForm.cs, app.manifest, Package.ps1 und Anleitungen
   gemeinsam aktualisieren. Build-Eingaben/Hashes müssen zur geprüften Mod passen.
   UpdateTests.cs verwendet die aktuelle Version für seine lokale Paket-Fixture.
3. `Package.ps1` ausführen. Es baut, testet und erstellt das fertige Paket.
4. Ein neues GitHub-Release anlegen; alte Release-Dateien nicht still austauschen.
   Ein eindeutiger Tag wie `helper-v0.1.5` ist möglich. Testversionen als Pre-release
   kennzeichnen. Einen vorhandenen veröffentlichten Tag nicht verschieben.
5. Die fertige Datei **Extended-Hotbar-Helper-X.Y.Z-test.zip** hochladen. Für eine
   geprüfte stabile Version ist **Extended-Hotbar-Helper-X.Y.Z.zip** erlaubt.
   Den gesamten Paketinhalt unverändert lassen. Keine zusätzliche äußere Ordnerebene.
   ZIPs mit Testspielstand werden absichtlich nicht als Update verwendet.
6. Zusätzlich die von `Package.ps1` erstellte **ZIPNAME.zip.update.json** hochladen,
   zum Beispiel `Extended-Hotbar-Helper-0.1.5-test.zip.update.json`. Sie enthält
   signierte Paketdaten, nicht den privaten Schlüssel. Ohne diese zweite Datei
   bietet der Helper kein ausführbares Update an. Teststatus im Release muss zum
   signierten Paket passen: -test.zip als Pre-release, stabiles .zip als Release.
7. Upload abschließen und Release veröffentlichen. GitHub muss in beiden Assets eine
   SHA256-Prüfsumme (`digest`) und den abgeschlossenen Zustand liefern. Ein manuell
   hochgeladenes SHA256SUMS.txt ist zusätzlich nützlich, ersetzt dieses Feld aber nicht.
8. Von einem älteren Helper aus prüfen: kurzer Hinweis, Download bestätigen,
   Windows-Prüfung zulassen, neuer Helper öffnet mit den gewählten Ordnern,
   Mod-Installation bestätigen. Danach Spieltest mit einer Testkampagne.

Es werden die neuesten 100 veröffentlichten Releases geprüft. Der Release-Tag
darf Buchstaben, Ziffern, Punkt, Unterstrich und Bindestrich enthalten. Die Auswahl
richtet sich nach der Helper-Version im Paketnamen, nicht nach dem Tag oder dem
„Latest“-Label. Stable-only blendet sowohl Pre-releases als auch -test.zip aus.

## Grenzen

Ein neuer Helper wird separat unter `%LOCALAPPDATA%\ExtendedHotbarHelper\Updates`
abgelegt. Die alte EXE bleibt erhalten; Desktop-/Steam-Verknüpfungen ändern sich
nicht. Nach dem Wechsel die neue EXE verwenden. Ohne gültigen Download wird kein
neuer Code gestartet. Spiel/Loader/Spielstände sind nicht im Updatepaket enthalten.

Die Prüfungen schützen vor beschädigten, falschen und unvollständigen Paketen.
Die zusätzliche RSA-4096/SHA256-Update-Signatur schützt auch dann, wenn jemand
das ZIP samt GitHub-Prüfsumme austauscht, solange der private Veröffentlichungsschlüssel
nicht kompromittiert ist. Sie ersetzt keine Windows-Herausgeber-Signatur.
Windows-Sicherheitsprüfungen und Zertifikats-Sperrprüfungen bleiben aktiv.

## Veröffentlichungsschlüssel — nicht hochladen

Der Schlüssel wurde einmalig mit `Init-UpdateSigning.ps1` angelegt. Nur die Datei
`UpdateTrust.xml` enthält den öffentlichen Prüfschlüssel und darf ins Repository.
Der private Schlüssel liegt Windows-kontogebunden verschlüsselt unter
`%LOCALAPPDATA%\ExtendedHotbarHelper\PublisherKeys\release-signing.dpapi`.
Die Ordnerrechte sind auf dein Windowskonto und SYSTEM beschränkt. Das verhindert
keinen Missbrauch durch Schadsoftware, die bereits unter deinem Konto läuft.

**Vor der öffentlichen Freigabe eine geeignete Schlüssel-/System-Sicherung planen.**
Eine Kopie der .dpapi-Datei allein ist keine portable Sicherung: Entschlüsselung
benötigt das bisherige Windowskonto samt DPAPI-Schlüsselmaterial. Niemals einen
unverschlüsselten privaten Schlüssel in GitHub, Chat-Anhänge, Release-Pakete oder
Cloud-Synchronisationsordner legen. Automatisch wurde keine externe Sicherung erstellt.

Verlust des Schlüssels verhindert weitere Updates für bereits verteilte Helper.
Für einen geplanten Wechsel muss noch mit dem alten Schlüssel ein neuer Helper
signiert werden, der den neuen öffentlichen Schlüssel enthält. Bei vermutetem
Schlüsselverlust oder Kompromittierung Veröffentlichungen stoppen und eine geprüfte
Migration beziehungsweise vertrauenswürdige manuelle Neuinstallation organisieren.

Signaturen gelten maximal 180 Tage. Abgelaufene Freigaben und stark falsche
Systemuhren werden abgelehnt; rechtzeitig eine neue, höhere Version veröffentlichen.
Das Ausblenden von Updates durch einen Netzangreifer lässt sich damit nicht
ausschließen. Auch der allererste Helper-Download braucht eine vertrauenswürdige Quelle.

Eine funktionierende öffentliche Veröffentlichung und der vollständige Wechsel
zwischen zwei tatsächlich veröffentlichten Versionen müssen vor der allgemeinen
Freigabe noch geprüft werden. Lokale simulierte Tests allein belegen das nicht.
