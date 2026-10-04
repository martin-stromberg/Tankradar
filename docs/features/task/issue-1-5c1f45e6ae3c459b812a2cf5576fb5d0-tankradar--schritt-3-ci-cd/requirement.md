# Anforderung (Nachbesserung Schritt 3, Runde 3)

- Das Skript `scripts/test-ios-deployment.ps1` muss unabhaengig von geerbten `Include*Target`-Umgebungsvariablen deterministisch sein (alle vier Eigenschaften je `-p:` explizit), damit der Pflichtjob `static checks` gruen ist.
- Nachweis: gruen ohne und mit `IncludeIosTarget=false`/`IncludeMacCatalystTarget=false` (und `IncludeAndroidTarget=false`), belegt in `scripts/local-ci.ps1`.
- Windows-Jobs: zusaetzlich `IncludeAndroidTarget=false` (Tankradar hat kein Android-Ziel); lokal Restore/Build/Test/Format/Security gruen.
- Pack-Cleanup in `package-ios`: an Vorlage angleichen oder per Kommentar begruenden.
