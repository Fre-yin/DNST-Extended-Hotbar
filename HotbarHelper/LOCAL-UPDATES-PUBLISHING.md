# Lokale Mod-Updates ab Helper 0.1.6

Der Nutzer lädt weiterhin selbst eine Mod-ZIP herunter. Der Helper findet sie im
Windows-Downloadordner (auch wenn dieser auf ein anderes lokales Laufwerk verlegt
wurde). Er sucht beim Start und unmittelbar vor dem Installieren erneut. Die ZIP
muss nicht entpackt und nicht im Helper ausgewählt werden. Downloads in beliebigen
anderen Ordnern werden nicht automatisch durchsucht. `.part`/`.crdownload`,
Quellcode-ZIPs werden ignoriert. Neuere Helper werden separat erkannt (siehe unten).
Ein erkannter Fehler wird angezeigt;
der Nutzer darf ausdrücklich auf die enthaltene Mod ausweichen, solange dies kein
Downgrade wäre. Unbekannte Spielbuilds werden dabei nicht freigegeben.

## Bereits vorhandene Pakete

Die enthaltenen 0.3.8-ZIPs für den ausgewählten Loader funktionieren ohne neue
Prüfdatei, solange ihre drei installierbaren Mod-Dateien exakt den eingebauten
Dateien entsprechen. Demo-Spielstände und alle anderen ZIP-Dateien werden niemals
automatisch importiert. Die originale ZIP und DLL werden nicht umgebaut.

## Eine spätere Mod-Version vorbereiten

1. Mod und Spielkompatibilität tatsächlich prüfen. Eine neue ZIP oder ein höherer
   Dateiname allein beweist keine Kompatibilität.
2. Mod-ZIP mit der richtigen Versionsnummer bauen; der normale Mod-Build prüft
   DLL-Identität und Version. Bei neuen Spielbuilds oder geändertem Save-Schema
   ist weiterhin ein überprüfter neuer Helper nötig.
3. Einen **neuen, leeren Ausgabeordner** anlegen und dort eine zweite ZIP erstellen:

```powershell
./Prepare-LocalModUpdate.ps1 -PackagePath 'C:\Build\Extended-Hotbar-0.3.8.zip' -OutputPath 'C:\Freigabe\Extended-Hotbar-0.3.8.zip'
```

Die Ordner sind Beispiele. Nichts wird durch dieses Skript veröffentlicht.
Für BepInEx zusätzlich -Loader BepInEx übergeben; Dateiname:
Extended-Hotbar-BepInEx-X.Y.Z-bepinex.N.zip. Sein Signaturzweck lautet
ExtendedHotbar.Mod.BepInEx.v1 und seine drei Pfade liegen unter BepInEx/plugins/ExtendedHotbar.
MelonLoader bleibt bei ExtendedHotbar.Mod.v1 und Mods/. Eine Signatur der
anderen Loader-Ausgabe wird nicht akzeptiert.
Das Skript verwendet ausschließlich den lokalen Veröffentlichungsschlüssel und
übernimmt die geprüften Spiel-Fingerprints und Mindest-Helper-Version aus dem
aktuellen Helper-Quellcode. Niemals diese Werte nur zum Umgehen einer Sperre ändern.
Es wird nichts hochgeladen. Vorhandene Archive werden nicht überschrieben.

4. Die erzeugte ZIP enthält zusätzlich `ExtendedHotbar.update.json`. Die
   RSA-4096/SHA256-Signatur bindet Mod-Version, Projekt, Mindest-Helper,
   Spiel-Fingerprints und SHA256-Werte der drei installierbaren Dateien.
   Der Nutzer braucht nur **diese eine ZIP**, keine zweite Prüfdatei.
5. Vor Freigabe Downloadordner-Erkennung, Installation, Sicherung und Rücknahme
   in einer Testinstallation prüfen; anschließend einen echten Spieltest machen.

Signieren macht ungeprüften Code nicht zuverlässig oder harmlos. Es bestätigt die
Freigabe durch den Besitzer des privaten Schlüssels. Der private Schlüssel wird
nicht in die ZIP aufgenommen. Das Skript erzeugt keine Windows-Authenticode-Signatur.

## Den Helper selbst lokal aktualisieren

Ein neuerer Helper hat Vorrang vor einer separaten Mod-ZIP. Dadurch kann er zuerst
seine neuere Kompatibilitätsprüfung und enthaltene Mod übernehmen. Er wird erst
nach Bestätigung, Signatur-/Hash-/Paketprüfung und EXE-Identitätsprüfung gestartet.
Die alte EXE bleibt erhalten; der neue Helper liegt unter
`%LOCALAPPDATA%\ExtendedHotbarHelper\Updates`. Danach dort die neue EXE verwenden;
bestehende Desktop-/Steam-Verknüpfungen werden nicht automatisch angepasst.
Mod-Installation erfordert im neuen Helper eine weitere Bestätigung.

Für die Verteilung eines neuen Helpers `Package.ps1 -SignUpdatePackage` verwenden.
Der Nutzer benötigt **zwei Dateien nebeneinander im Downloadordner**:

- `Extended-Hotbar-Helper-X.Y.Z-test.zip`
- `Extended-Hotbar-Helper-X.Y.Z-test.zip.update.json`

Die zweite Datei enthält nur signierte Paketdaten, nicht den privaten Schlüssel.
Namen unverändert lassen. ZIP allein oder eine selbst erstellte Prüfsumme reicht
nicht. Alte Helper-Pakete mit `-mit-Testspielstand` im Namen, unvollständige Dateien und gleich alte/ältere
Helper werden nicht als Selbstupdate angeboten. Die Signatur bindet das gesamte
ZIP samt Version, Größe und Teststatus und ist maximal 180 Tage gültig. Eine falsche
Uhr oder abgelaufene Freigabe verhindert den Start. Windows-Herausgeberwarnungen
werden nicht unterdrückt. Dieser Ablauf verwendet keinen GitHub-Zugriff.

Ab 0.1.12 stecken beide loader-spezifischen Mod-Pakete in der Helper-EXE.
Nur das MelonLoader-Paket enthält den optionalen Testspielstand. Keine zusätzlichen Demo-Varianten veröffentlichen. Das äußere
Helper-ZIP behält exakt 21 Dateien, damit ältere Helper es weiterhin prüfen können.
Der MelonLoader-Mod-Download enthält ebenfalls den Testspielstand. Er wird niemals
automatisch importiert; der Helper kann das enthaltene ZIP auf Wunsch speichern.

## Online bleibt aus

`Updates.OnlineEnabled` ist in 0.1.12 fest `false`; die Oberfläche hat keinen
Online-Schalter und ruft keinen Netzwerk-Client auf. Auch ein direkter Aufruf des
vorbereiteten Clients wird vor einer Verbindung abgelehnt. Alte Online-Einstellungen
werden nicht verwendet. `Package.ps1` greift standardmäßig nicht auf den privaten
Schlüssel zu; die optionale Paket-Signierung ist ein separater Entwickler-Schritt.

Hardware-Schlüssel, Wiederherstellungsplan und vollständiger GitHub-Test folgen
erst später. Ein FIDO-Anmeldeschlüssel ersetzt nicht automatisch den RSA-Schlüssel
für diese Mod-Freigaben. Bereits ausgelieferte öffentliche Prüfschlüssel erfordern
bei einem Wechsel eine geplante Migration. Keine Sichtbarkeit oder Veröffentlichung
wurde durch diese Arbeit geändert.
