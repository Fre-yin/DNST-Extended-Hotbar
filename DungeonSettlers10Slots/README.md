# Extended Hotbar 0.3.9 – für Dungeon Settlers

Lokaler Update-Kandidat für **DS_B.0.4.23 / Steam-Build 25269660**, Windows x64, MelonLoader 0.7.3. Gegen neu erzeugte Spiel-Schnittstellen gebaut und mit nativen Laufzeittests in einer getrennten Spielkopie geprüft. Sichtbarer Kampf und lange Kampagnen sind noch nicht abgenommen. **DungeonSettlers10Slots.dll**, Speicher-/Binding-Kennungen, zehn aktive Skills plus Basisangriff und drei Itemslots bleiben unverändert. Keine Zwölf-Slot-Oberfläche. Frühere Versions- und Abnahmeberichte sind historische Stände, keine Abnahme dieses Patches.

## Lizenz und unterstützter Loader

Der eigenständig verfasste Modcode, Build-Skripte und eigene Anleitungstexte stehen unter **MIT**, siehe [LICENSE](LICENSE). Das ist keine freie Lizenz für das gesamte gemischte Paket: **SkillFrame__sharedassets0_mod_4898.png ist eine von Danny bearbeitete Originalgrafik aus Dungeon Settlers und ausdrücklich nicht MIT-lizenziert.** Der Rechtehinweis [Assets/NOTICE.txt](Assets/NOTICE.txt) wird direkt neben der PNG mitgeliefert. Auch der optionale Testspielstand und darin repräsentierte Spielinhalte werden nicht unter die MIT-Code-Lizenz gestellt. Rechteumfang und Herkunft: [LICENSING.txt](LICENSING.txt), [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt). Original-Lizenztexte der referenzierten Bibliotheken liegen unter `Licenses`; ihre DLLs werden nicht verteilt.

Inoffizielle Community-Mod, kein offizielles oder als solches genehmigtes Produkt. Unsere Code-Lizenz erteilt keine Rechte am Spiel und hebt die [Dungeon-Settlers-EULA](https://store.steampowered.com/eula/2798330_eula_0) nicht auf.

Dieses Paket verwendet **MelonLoader 0.7.3**. Eine eigene BepInEx-6-Unity-IL2CPP-x64-Ausgabe wird getrennt gebaut. Zwei Loader übereinander zu installieren oder die DLL in den jeweils anderen Mod-Ordner zu kopieren ist keine freigegebene Installationsmethode. Andere Mods sind nicht pauschal als kompatibel geprüft.

Frühere Paketrevisionen bleiben unverändert erhalten. Neue Pakete werden hier nur lokal vorbereitet; GitHub-/Nexus-Veröffentlichungen und Commits übernimmt Danny manuell.

## Umfang

- Zehn aktive Skills plus separater Basisangriff.
- Originale und bereits gespeicherte Charakterbelegungen bleiben erhalten.
- Drei Itemslots; zusätzliche Skill- und Item-Aktionen zunächst ohne Taste.
- Sechs zusätzliche Skill- und zwei zusätzliche Itemzeilen im originalen Optionenmenü, jeweils mit zwei Belegungsfeldern.
- Native Drag-, Tooltip-, Autoskill-, Inventar-, Kosten- und Cooldown-Verarbeitung.
- Unveränderter Benutzerrahmen, kompakte Itemgruppe und erhaltenes linkes Charakterfenster. Keine QA-Funktionen im Hotbar-Paket.

Belege die zusätzlichen Aktionen selbst in den Spieloptionen. Es wird kein Zahlen-, Q/E/R- oder Shift-Profil angewendet. Bestehende gespeicherte Belegungen werden weder beim Start ersetzt noch automatisch von Konflikten bereinigt. Achte beim Bestätigen einer Neubelegung auch auf Aktionen anderer Kontexte, etwa Q/E zum Gebäudedrehen.

## Sprachen ab 0.3.3

Die Zusatzzeilen **Skill 5–10 und Item-Quickslot 2–3** verwenden den Wortlaut der originalen Spielübersetzungen mit fortgesetzter Nummerierung. Das gilt auch für ihre Namen im nativen Belegungskonflikt-Dialog. Ein Sprachwechsel baut die zusätzlichen Textkeys neu auf; keine dauerhafte Bindung an Deutsch oder die Windows-Sprache. Originalzeilen, Skill-/Itemnamen und Kontextmenüs bleiben vom Spiel lokalisiert.

Unterstützt werden alle zehn im geprüften Spielbuild auswählbaren Sprachen: Englisch, Koreanisch, Französisch, Deutsch, Russisch, vereinfachtes und traditionelles Chinesisch, Japanisch, Spanisch und brasilianisches Portugiesisch. Koreanische Item-Zusatznamen bekommen eine angehängte Nummer, weil der native Einzelplatz unnummeriert ist. Fehlende beziehungsweise TODO-Texte verwenden den englischen Spieltext, zuletzt einen englischen Notfalltext. Noch nicht auswählbare TODO-Spalten der Originaltabelle sind keine fertig unterstützten Spielsprachen.

Die Registrierung betrifft ausschließlich acht mod-eigene Textkeys im Arbeitsspeicher. Keine Übersetzungsdateien oder Originaltexte werden überschrieben. Das native Menü übernimmt wieder vollständig Beschriftung und beide Belegungsknöpfe; die alte deutschsprachige Sonderbehandlung entfällt. Auch ausgeblendete Zusatzzeilen erhalten beim Aktualisieren den passenden nativen Sprachfont. Ab 0.3.5 werden vorher mit StoreInitialDefaults die nativen Darstellungsvorgaben gesichert: sonst kann ein Refresh vor Awake die sichtbare Zeilenzahl auf null setzen. Schrift-/Layoutdarstellung aller Sprachen ist weiterhin visuell abzunehmen.

Der erzeugte Rahmen und seine Textur sind außerdem gegen Unitys Freigabe unbenutzter Assets geschützt. Die frühere Einmal-Ladesperre konnte nach Sprach-/Kampagnenwechseln den breiten Rahmen dauerhaft verlieren; verlorene Laufzeitobjekte werden nun bei Bedarf neu geladen. Echte Dateifehler bleiben gegen wiederholte Ladeversuche gesperrt. Es werden nur die beiden mod-eigenen Assets gehalten und beim Beenden ausdrücklich freigegeben.

## Installation in der Steam-Version

1. Spiel und eventuell laufende Testkopie vollständig beenden. Den gesamten Ordner `%USERPROFILE%/AppData/LocalLow/CanOpener/Dungeon Settlers` an einen sicheren Ort kopieren. Originalinstallation und Testkopie teilen Spielstände und Einstellungen.
2. Den [offiziellen MelonLoader-Installer für Windows](https://github.com/LavaGang/MelonLoader.Installer/releases/latest) herunterladen und öffnen. Dungeon Settlers auswählen; falls es nicht angezeigt wird, über **Add Game Manually** die `DungeonSettlers.exe` aus dem Steam-Spielordner auswählen. **MelonLoader 0.7.3**, keine Nightly-Version, installieren. Den Loader nur einmal pro Spielinstallation einrichten.
3. Über **Ordner öffnen** im Installer den Spielordner öffnen. Alternativ in Steam die lokalen Spieldateien durchsuchen. Gemeint ist der Ordner mit `DungeonSettlers.exe`, nicht der Spielstandordner.
4. Das Hotbar-ZIP über **Alle extrahieren** entpacken. Den enthaltenen Ordner `Mods` in den Spielordner kopieren; einen vorhandenen Mods-Ordner zusammenführen. Nicht versehentlich `Mods/Mods` erzeugen. Sowohl `Mods/DungeonSettlers10Slots.dll` als auch der mitgelieferte Ordner `Mods/DungeonSettlers10SlotsAssets` müssen mitkommen. QA oder andere Hilfsmods sind nicht nötig. Bei einem Update vorher die alte Hotbar-DLL aus dem Mods-Ordner nehmen, siehe unten.
5. Ganz normal über Steam starten, ohne Diagnose-/Preset-Schalter. Die erste Einrichtung kann länger dauern. Beim bestätigten Benutzer-Installationstest war die Mod nach der ersten Einrichtung und einem erneuten Start aktiv. Anschließend eine Testkampagne laden und einen Charakter auswählen: zehn Skills plus Basisangriff, drei Itemfelder und zusätzliche Belegungszeilen in den Optionen prüfen.

Bestehende Tastenbelegungen werden nicht durch die Installation ersetzt. Für eine Fehlersuche enthält `MelonLoader/Latest.log` den Kompatibilitätsnachweis und bei erfolgreichem Start „10-slot release patches loaded“.

### Update

Spiel schließen und Spielstände sowie bisherige Hotbar-DLL und den Grafikordner sichern. **Beim Wechsel von 0.3.3 oder älter die bisherige DungeonSettlers12Slots.dll aus dem Mods-Ordner in die Sicherung verschieben.** Danach nur den Mods-Inhalt des neuen Pakets wie oben kopieren. Genau eine Hotbar-DLL behalten, jetzt DungeonSettlers10Slots.dll. Alte und neue DLL dürfen nicht gleichzeitig geladen werden. Spielstandformat, Mod-Textkeys, Binding-IDs und Item-Erweiterungsname bleiben unverändert. Kein Reset und keine Spielstandmigration nötig.

Das MelonLoader-Paket enthält Hotbar-DLL, bearbeitete Original-PNG, deutsche/englische Anleitung, Lizenzhinweise und vier Bibliotheks-Lizenztexte. Der getrennte, freiwillige Testspielstand hat eigene Importanleitungen. Entwicklerberichte und Markdown-Dateien bleiben außerhalb dieses ZIPs. Keine Spielassemblies, Loader, Benutzereinstellungen oder QA-Mod. Der Paketbau prüft DLL-Identität sowie alle fünfzehn ZIP-Einträge und deren Prüfsummen.

### Optionaler Testspielstand

Das MelonLoader-Paket enthält optional den am **06.09.2026, 11:21:32** aktualisierten **10 Slots Testfile**, gespeichert mit **DS_B.0.4.17**. **Def not Frieren** hat zehn unterschiedliche gelernte Skills und drei unterschiedliche Itemtypen zugewiesen; von den drei zugewiesenen Itemtypen sind jeweils fünf im Inventar. Der Stand startet pausiert; Autoskill ist eingeschaltet. Die originale Datei wird unverändert unter `Testspielstand/Saves/10SlotsTestfile.json` mitgegeben. Der Import ist freiwillig und in zwei normalen Textdateien erklärt. Keine Einstellungen, Belegungen, globalen Cachedateien oder anderen Spielstände enthalten. Nur `Mods` gehört neben die Spiel-EXE; die Testdatei gehört in den persönlichen **Saves**-Ordner. Bestehende gleichnamige Dateien nicht ungesichert überschreiben. Das BepInEx-Paket enthält keinen Testspielstand.

Bei unbekanntem Spielcode oder abweichenden Metadaten deaktiviert sich die Erweiterung. **Dann modifizierte Kampagnen nicht weiter speichern.** Vanilla beziehungsweise eine deaktivierte Mod kann Zusatzdaten nicht zuverlässig erhalten. Für Rückkehr zu Vanilla eine Sicherung von vor der Mod verwenden; Entfernen der DLL ist keine Savegame-Migration.

## Schutzmaßnahmen in 0.3.2

- Alte Vier-Slot-Daten werden erweitert. Längere Teststände behalten versteckte Plätze 11/12 im Save; die Anzeige erhält nur zehn aktive Einträge.
- Item 1 bleibt im nativen Datensatz. Items 2/3 stehen unter DungeonSettlers10Slots_Items, Version 1, je Charakter-GUID.
- Unbekannte Erweiterungsversionen bleiben unverändert erhalten; Zusatzfelder sind dann schreibgeschützt.
- Nicht lesbare/mehrdeutige Daten oder fehlende Lade-Zuordnungen sperren das Weiterspeichern, statt leere Daten zu schreiben. Spielstand erneut laden und das Fehlerprotokoll prüfen.
- Bei fehlgeschlagener JSON-Ergänzung werden nativer Schreibaufruf und Save-Header-Aktualisierung übersprungen. Der originale atomare Schreibweg bleibt erhalten.
- Begrenzter Snapshot-Cache mit ausdrücklicher FIFO-Reihenfolge; verlorene Zuordnungen werden nicht als leere Itembelegung behandelt.
- Lebende Hotbar-Registrierungen bleiben bei zusätzlichen Szenen erhalten, damit spätere Umbelegungen weiter angezeigt werden.
- Optionszeilen entstehen vor dem nativen RefreshRows. Den früheren Init-Eingriff umgingen eingebettete native Aufrufpfade.
- Diagnoseobjekte und Tastaturtests laufen nur auf expliziten Prüfstarts. Diagnoseflags werden einmal gelesen. Kein periodischer UI-Scan; bei zehn Skill-Einträgen wird kein neues Anzeige-Array erzeugt. Layout und Benutzer-PNG wurden nicht umgestaltet.

## Grenzen und Abnahme

Der native Belegungsdialog nimmt einzelne Tasten auf, keine frei eingegebenen Modifier-Kombinationen. Bereits vorhandene Modifier-Belegungen bleiben erhalten, werden aber nicht von der Mod erzeugt. Lange Kürzel und schmale Auflösungen sind gesondert zu prüfen.

Der frühere kurze Helligkeitstick ist nicht abschließend als behoben nachgewiesen. Langzeittest, Dungeonwechsel, unterschiedliche Itemarten und mehrere UI-Skalierungen bleiben Teil der Release-Abnahme. Daher **Release-Kandidat**, keine pauschale Fehlerfreiheitszusage.

Aktuelle Patch-Belege: PATCH-25269660.md. RELEASE-REVIEW.md und DEVELOPMENT-HISTORY.md enthalten frühere Zwischenstände; deren historische Installationshinweise gelten nicht automatisch für die aktuelle Version.

## Bauen und prüfen

Aus dem Workspace, mit dem Pfad einer auf DS_B.0.4.23 aktualisierten MelonLoader-Spielkopie:

    $gameDir = 'C:\Pfad\zur\aktualisierten\Spielkopie'
    dotnet build DungeonSettlers10Slots/DungeonSettlers10Slots.csproj -c Release "-p:GameDir=$gameDir"
    dotnet restore tests/Hotbar.Tests/Hotbar.Tests.csproj --configfile tests/Hotbar.Tests/NuGet.Config
    dotnet run --project tests/Hotbar.Tests -c Release --no-restore
    pwsh -File DungeonSettlers10Slots/Build-Release.ps1 -GameDir $gameDir

Benötigt frisch zum Zielbuild erzeugte IL2CPP-Referenzen und MelonLoader im angegebenen Spielordner. Alte Sandbox-Referenzen sind kein Ersatz. Die paketfreien Tests laufen unter .NET 6 ohne Spielstart. Vor der ersten Paketierung den Build und Test-Restore wie oben ausführen. Der Paketbau verwendet standardmäßig ausschließlich die geprüfte Kopie unter `TestSave/Saves` und prüft ihren Codec-Rundlauf. `-IncludeTestSave` bleibt für bisherige Aufrufer erhalten; eine neue Ausgabe ohne Testsave wird nicht erzeugt. Kein automatischer Zugriff auf ein Benutzerprofil. `-PackageRevision 1` kennzeichnet die reine Unterlagen-Revision als `-r1`, ohne die Mod-DLL-Version zu ändern. Ohne diesen Schalter bleibt das ursprüngliche Namensschema erhalten. Vorhandene Release-Ausgaben werden nicht überschrieben.

--ds-run-audits oder --ds-run-item-audits aktiviert native Speicher-, Kontext- und Tastaturprüfungen. Die vollständige Tastaturmatrix muss ohne andere Mods mit Eingabefiltern laufen: jede reservierte QA-Taste blockiert sie absichtlich. F9 nach F8 zu verschieben ersetzt keine Testisolation. --ds-run-ui-audits ergänzt invasive temporäre UI-Klontests und ist nicht zur Performancebeurteilung geeignet.

Zusammen mit einem Audit-Schalter führt --ds-audit-save-fixture=<absoluter JSON-Pfad> einen echten SaveFile-/Disk-/Lade-Test einer Sicherung aus. Er schreibt nur einen neuen DSHotbarAudit-…-Ordner im Temp-Verzeichnis; der gemeinsame Save-Header-Cache wird ausschließlich während des Tests umgangen.

--ds-run-localization-audits prüft einmal nach Bereitstellung der Spieldaten alle zehn Sprachen in separaten nativen Texttabellen sowie die nativen Optionszeilen, beide Rückrufe und Konfliktnamen in der aktuellen Sprache. Inaktive Testobjekte reproduzieren außerdem die frühere unsichtbare Textdarstellung und prüfen erhaltene Umbruch-/Zeilenlimits. Beim Öffnen der echten Optionen werden die acht geklonten Zeilen und ihre 24 Textfelder ausschließlich lesend kontrolliert. Keine Kampagnenaktion und kein Speichern oder Umstellen der Benutzersprache. Kein Schalter für normale Starts erforderlich.

--ds-run-frame-lifetime-audit prüft einmal beim frischen Start ohne geladene Kampagne die Asset-Freigabe und Wiederherstellung. Dabei wird die alte Schutzmethode auf Wegwerfobjekten reproduziert und der Benutzerrahmen gegen Resources.UnloadUnusedAssets geprüft. Während dieses kurzen Tests im Hauptmenü bleiben; der Schalter gehört nicht in den normalen Launcher.
