# Projektplan: Tankradar – Kraftstoffpreise, Favoriten, Routensuche und Tankbuch

## Übersicht

Tankradar ist eine neu zu entwickelnde .NET-MAUI-App (iOS primär, Windows für Entwicklung,
Debugging und automatisierte Oberflächentests). Sie ruft aktuelle Kraftstoffpreise ab, findet
Tankstellen per Standort, Adresse oder entlang einer Route, verwaltet favorisierte Tankstellen in
Gruppen und bietet ein offline nutzbares Tankbuch mit Verbrauchs- und Kostenauswertung pro Fahrzeug
einschließlich Export. Strompreise für Ladestationen sind nicht Teil von Version 1.0, weil es keine
offizielle, frei nutzbare Strompreis-API gibt (Stakeholder-Entscheidung). Das Repository enthält
bisher nur Anforderung, Designentwurf und Dokumentation. Deshalb werden neben den fachlichen
Funktionen auch Projektgrundgerüst, lokale Git-Hooks, CI/CD-Pipeline, iOS-Auslieferung und die
Ablage startfähiger Windows-Zwischenstände aufgebaut.

## Grobe Vorgehensentscheidungen

| Bereich | Entscheidung | Begründung |
|---------|-------------|------------|
| Reihenfolge | Zuerst die Infrastruktur (App-Grundgerüst mit Design-System, Git-Hooks, CI/CD), danach die fachlichen Funktionen. | Alle folgenden Schritte laufen so von Anfang an durch dieselben Qualitätsprüfungen. Die Anforderung verlangt Hooks und CI als verbindlichen Qualitätsfilter. |
| Schnitt der Schritte | Jeder fachliche Schritt liefert Datenhaltung, Logik, Oberfläche und Tests (Unit, Integration, FlaUI-E2E) zusammen als vertikalen Schnitt. Eine nachgelagerte reine Test-Phase gibt es nicht. | Jeder Schritt muss eigenständig über `/lifecycle` lieferbar und testbar sein. Die in der Anforderung skizzierte Aufteilung nach technischen Schichten wäre nicht einzeln abnehmbar. |
| Plattformen | Entwickelt und automatisiert getestet wird auf Windows. iOS wird ab dem CI-Schritt mitgebaut. Die iOS-Auslieferung (Signierung, TestFlight) ist ein eigener Schritt. | Die lokale Toolchain ist Windows. iOS-Builds benötigen macOS. |
| Mindestversion iOS | iOS 16. | Stakeholder-Entscheidung (Empfehlung zu offenem Punkt 13 übernommen). |
| Apple-Kennungen | Bundle-ID, Team-ID und Signierungsdaten werden als Konfiguration bzw. Secrets vorgesehen (GitHub Secrets und lokale Umgebungsvariablen), nie im Repository. Der Anwender liefert die Werte später. Bis dahin laufen alle Schritte ohne diese Werte; nur Signierung und Store-Upload setzen sie voraus. | Stakeholder-Entscheidung (offener Punkt 13). Fehlende Kennungen sollen die Umsetzung nicht blockieren. |
| Datenhaltung | Nutzer- und Abrufdaten liegen in einer einzigen lokalen SQLite-Datenbank, die mit jedem Schritt erweitert wird. Schemaänderungen erfolgen so, dass bestehende Installationen beim Update ihre Daten behalten. | Vorgabe der Anforderung („keine getrennten Datenbanken"). Spätere Updates dürfen Tankbuch und Favoriten nicht verlieren. |
| Schutz lokaler Daten | API-Zugangsdaten liegen in der sicheren Ablage des Betriebssystems (iOS: Keychain, Windows: Credential Locker). Datenbank und Belegfotos werden über den systemseitigen Dateischutz geschützt (iOS Data Protection). Eine zusätzliche Verschlüsselung der Datenbank erfolgt nicht. | Stakeholder-Entscheidung (offener Punkt 11). |
| Kraftstoffpreis-Quelle | Tankerkönig-API (offizielle Daten der Markttransparenzstelle für Kraftstoffe). Die App zeigt die Quellenangabe gemäß Lizenz CC BY 4.0 an und hält sich an die Nutzungsbedingungen des Anbieters (Abrufe drosseln, Cache nutzen). Den API-Schlüssel beantragt der Anwender. Er gelangt beim Build aus einem Secret bzw. einer nicht versionierten lokalen Konfiguration in die App und liegt zur Laufzeit in Keychain bzw. Credential Locker, nie im Code. | Stakeholder-Entscheidung (offener Punkt 1). |
| Fehlende Quelldaten | Angezeigt wird nur, was die Quelle liefert; fehlende Angaben (z. B. Zahlungsmöglichkeiten) werden ausgeblendet. „Automatentankstelle" wird, wo möglich, aus den Öffnungszeiten (durchgehend geöffnet) abgeleitet, „Preis unbestätigt" aus dem Preisalter. | Stakeholder-Entscheidung (offener Punkt 3). |
| Preisaktualität | Das Preisalter wird immer als „vor X Min." angezeigt. Ab 60 Minuten gilt ein Preis als veraltet und wird in Amber markiert. Ohne Verbindung erscheint zusätzlich ein Offline-Hinweis in der Kopfzeile. | Stakeholder-Entscheidung (offener Punkt 4). |
| Geokodierung | Adressen, Orte und Postleitzahlen werden auf beiden Plattformen einheitlich über OpenStreetMap-Nominatim in Positionen umgewandelt. Dabei wird die Nutzungsrichtlinie eingehalten: Anfragen nur auf ausdrückliches Absenden, keine Autovervollständigung, höchstens eine Anfrage pro Sekunde, identifizierende Kennung der App, Quellenangabe. | Stakeholder-Entscheidung (offener Punkt 5). Gleiches Verhalten unter iOS und Windows, damit FlaUI-Tests aussagekräftig sind. |
| Karte | Eine plattformübergreifende Kartenkomponente auf Basis von OpenStreetMap-Kacheln, die unter iOS und Windows gleich funktioniert. Die Quellenangabe „© OpenStreetMap-Mitwirkende" wird angezeigt. | Stakeholder-Entscheidung (offener Punkt 6). Die Standard-Kartenkomponente von MAUI unterstützt Windows nicht, die Karte muss aber in FlaUI-Tests prüfbar sein. |
| Preisniveau-Farben | Die Farbe richtet sich nach der aktuellen Ergebnismenge: günstigster Preis Grün, Preise im oberen Drittel der Preisspanne Rot, alle übrigen Teal, geschlossene Stationen Grau. | Stakeholder-Entscheidung (offener Punkt 7). |
| Routing-Dienst | Routen werden über OpenRouteService (Directions, Profil Auto) ermittelt, ausschließlich über HTTPS. Der API-Schlüssel wird wie der Tankerkönig-Schlüssel behandelt: Bereitstellung über Secret bzw. nicht versionierte lokale Konfiguration, zur Laufzeit in Keychain bzw. Credential Locker, nie im Code. Start- und Zieladressen werden über dieselbe Geokodierung wie die Adresssuche aufgelöst. | Stakeholder-Entscheidung: Route bereits in Version 1.0 (offener Punkt 14). OpenRouteService passt zu den OSM-basierten Entscheidungen für Karte und Geokodierung, bietet einen gehosteten Dienst mit kostenlosem Kontingent und ist für den produktiven Einsatz vorgesehen. Der öffentliche OSRM-Demoserver ist das nicht. |
| Umfang Version 1.0 | Export (CSV/PDF) und Tankstellen entlang einer Route sind reguläre Schritte von Version 1.0, nicht mehr optional. Cloud-Synchronisation wird vor Version 1.0 nicht umgesetzt; bei Bedarf folgt sie später als eigener Schritt, nachdem der Anwender einen Anbieter festgelegt hat. | Stakeholder-Entscheidung (offene Punkte 12 und 14). |
| Keine Strompreise in 1.0 | Der Abruf von Ladestationen und Strompreisen (AC/DC/HPC) entfällt vollständig, ebenso alles, was davon abhängt: die Einstellung „Strompreis-Anzeige aktivieren" samt Auswahl der Ladetypen, der Filter nach Strom bzw. Ladetyp in Suche und Karte, Strompreise in Detailansicht, Startseite und Bereich „In der Nähe", Ladestationen als Favoriten, der Schalter „Kraftstoff/Laden" und die Ladestecker-Kennzeichnungen aus dem Designentwurf sowie die Sicherung von Strompreisständen. Ladestationen ohne Preise werden ebenfalls nicht angezeigt. Erhalten bleiben alle Funktionen, die keine Strompreisquelle brauchen: Fahrzeuge mit Kraftstoffart „Elektro", manuell erfasste Ladevorgänge in kWh im Tankbuch sowie Verbrauchs- und Kostenauswertung in kWh/100 km. Die ausgelassenen Designelemente werden als bewusste Abweichung vom Designentwurf in der Projektdokumentation festgehalten. | Stakeholder-Entscheidung zu offenem Punkt 2: „wenn es keine api gibt, dann eben ohne strompreise". In Deutschland gibt es keine offizielle, frei nutzbare Strompreis-API. Das Ladesäulenregister der Bundesnetzagentur liefert nur Standorte und Leistung. Die Ladestations-Funktionen der Anforderung drehen sich um Preise, eine reine Standortanzeige wäre eine nicht angeforderte Erweiterung. Ladevorgänge im Tankbuch gibt der Nutzer selbst ein, sie brauchen keine API. |
| Externe Dienste in Tests | Automatisierte Tests nutzen nie produktive Endpunkte (Kraftstoffpreise, Geokodierung, Routing, Kartenkacheln), sondern Mock-Server bzw. Testdaten. E2E-Tests laufen gegen eine eigene, isolierte Testdatenbank. | Vorgabe des Sicherheitskapitels. Tests sollen ohne API-Schlüssel und ohne Rate-Limits reproduzierbar sein. |
| Windows-Zwischenstände | Nach jedem Schritt mit sichtbarer Änderung wird ein startfähiger Windows-Stand unter `review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (mit kurzem Changelog). Das Verzeichnis ist per `.gitignore` von Commits ausgeschlossen. | Stakeholder-Entscheidung vom 2026-09-28. |
| Designentwurf | Der Designentwurf (Navigation „Favoriten/Start – Karte – Tankbuch – Optionen", Farben, Typografie, Hell- und Dunkelmodus) ist verbindlich. Jede Abweichung, auch der Wegfall der Strom-Elemente, wird in der Projektdokumentation festgehalten. | Vorgabe „Design als fester Bestandteil der Architektur". |
| Versionierung | Versionen folgen den Regeln der CI-Workflow-Vorlage (Conventional Commits, semantische Versionierung). Die erste Version ist 0.1.0; bis 1.0 wird die Hauptversion nicht automatisch erhöht. | Vorgabe „erste Testversionen beginnen bei 0.1". |
| CI-Verfügbarkeit | Zu jeder CI-Prüfung gibt es einen gleichwertigen lokalen Prüflauf. Scheitern GitHub Actions am Billing-Limit, gelten die lokalen Ergebnisse. | Das Repository bleibt bis 1.0 privat, Actions können daher scheitern. |
| Feature-Flags | Es gibt keinen eigenen Mechanismus für Feature-Flags. Die Routensuche ist fester Bestandteil von Version 1.0. Das Offline-Verhalten ergibt sich aus der tatsächlichen Verbindung und wird nicht per Schalter erzwungen. Den E2E-Testmodus steuert die ohnehin geplante Test-Konfiguration (isolierte Testdatenbank, Mock-Dienste statt produktiver Endpunkte). Strom- und Cloud-Schalter entfallen mit den zugehörigen Funktionen. | Entscheidung des Projektleiters vom 2026-09-28: Die Feature-Flags stehen nicht in der Originalanforderung (`issue.md`), sondern sind ein Übersetzungsartefakt. Ein zusätzlicher Schaltmechanismus hätte in 1.0 keinen fachlichen Nutzen. |
| Windows-Auslieferung | Für Windows gibt es keinen Installer (weder MSIX noch Setup.exe). Als Windows-Stände genügen die startfähigen Zwischenstände unter `review-versions/` und die CI-Release-Artefakte: ein gezipptes Build, das nach dem Entpacken ohne Installation startet. | Entscheidung des Projektleiters vom 2026-09-28: Laut Anforderung ist Windows Entwicklungs- und Testplattform. Der Installer steht nicht in der Originalanforderung, sondern stammt aus der Übersetzung. |
| Sprache | Die Oberfläche ist deutsch. Oberflächentexte werden zentral gepflegt, damit die Übersetzungsprüfung der Git-Hooks greift. | Zielgruppe und Designentwurf sind deutsch. |

## Entwicklungsschritte

### Schritt 1: App-Grundgerüst, Navigation und Design-System

**Beschreibung:** Für die App „Tankradar" soll das Projektgrundgerüst als .NET-MAUI-Anwendung mit
einer gemeinsamen Codebasis für iOS (primäre Zielplattform, Mindestversion iOS 16) und Windows
(Entwicklung, Debugging, automatisierte Tests) angelegt werden. Die App folgt einer MVVM-Struktur.
Nach dem Start zeigt sie die im Designentwurf (`design-draft/stitch_smart_fuel_charge_tracker.zip`)
vorgesehene Hauptnavigation mit den Bereichen „Favoriten" (Startseite), „Karte" (Suche), „Tankbuch"
und „Optionen" (Einstellungen). Die Bereiche sind zunächst leer, aber bereits im Design gestaltet.
Das Design-System des Entwurfs wird als zentral wiederverwendbare Gestaltungsgrundlage
eingerichtet: Farbpalette Teal/Emerald/Amber/Rot, Schriften Inter und JetBrains Mono, Abstände,
Eckenradien, Schatten, Mindestgröße 44×44 für Bedienelemente, Hell- und Dunkelmodus.
Strompreise und Ladestationen gehören nicht zum Umfang von Version 1.0. Die dafür gedachten
Elemente des Entwurfs (Umschalter „Kraftstoff/Laden", Ladestecker-Kennzeichnungen) werden daher
nicht umgesetzt; das wird als bewusste Designabweichung in der Projektdokumentation festgehalten.
Die App-Kennung (Bundle-ID) wird als Konfigurationswert vorgesehen, den der Anwender später
festlegt; bis dahin gilt ein Platzhalterwert, der die Entwicklung nicht blockiert. Die Windows-App
muss ohne Installation direkt startbar sein.

Zusätzlich werden die Testgrundlagen geschaffen: je ein Projekt für Unit-Tests, Integrationstests
und End-to-End-Tests mit FlaUI gegen die Windows-App. Ein erster E2E-Test startet die App und
wechselt durch alle Navigationsbereiche. E2E-Tests verwenden ein Datenverzeichnis, das vom
Entwicklungs- und Echtbetrieb getrennt ist. Außerdem wird ein einfach aufrufbares Verfahren
(z. B. ein Skript) bereitgestellt, das einen startfähigen Windows-Zwischenstand strukturiert unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root ablegt: klar benannt, datiert und mit
kurzem Changelog. Dieses Verzeichnis ist bereits per `.gitignore` von Commits ausgeschlossen und
darf nicht committet werden. Die Nutzung des Verfahrens wird in der Projektdokumentation
beschrieben. Zum Abschluss wird damit der erste Zwischenstand (Version 0.1.0) abgelegt.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Projektstruktur, Navigation, Design-System, Testinfrastruktur, Ablage von Zwischenständen

### Schritt 2: Lokale Git-Hooks zur Qualitätssicherung

**Beschreibung:** Im Repository sollen lokale Git-Hooks eingerichtet werden, die vor Commit und
Push sicherstellen, dass der Code konsistent, formatiert und fehlerfrei ist. Grundlage sind die
verbindlichen Vorlagen unter
`docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/inventory/external-sources/`
(`git-hooks-readme.md` und Ordner `githooks/`). Die Hooks liegen versioniert im Repository und
werden über ein mitgeliefertes Installationsskript (Windows und Unix-Shell) aktiviert.

Übernommen werden die Prüfungen der Vorlage, soweit sie für eine MAUI/XAML-App zutreffen:
XML-Dokumentation, Verbot von Platzhalter-Implementierungen, Abdeckung von Aufzählungswerten durch
Tests, Prüfung von Übersetzungen und Ressourcen sowie das Blockieren direkter Commits und Pushes auf
`main` und `staging`. Prüfungen, die nur für Razor gedacht sind, entfallen oder werden sinngemäß
ersetzt. Über die Vorlage hinaus verlangt die Anforderung:

- Prüfung von Formatierung und Code-Stil
- Prüfung auf verbotene Muster und Dateien, insbesondere API-Schlüssel (z. B. für Kraftstoffpreis-
  und Routing-Dienst), Zertifikate und Signierungsdaten, Datenbank-Dumps und Logdateien
- Validierung der Commit-Nachrichten im Conventional-Commits-Format (Voraussetzung für die
  automatische Versionierung)
- Sicherstellung, dass die Tests vor dem Push lokal ausgeführt wurden und erfolgreich waren

Installation und Wirkung der Hooks werden in der Projektdokumentation beschrieben. Auf dem aktuellen
Stand des Repositorys müssen die Hooks ohne Beanstandung durchlaufen.

**Abhängigkeiten:** 1

**Betroffene Bereiche:** Entwicklungsprozess, Codequalität, Repository-Sicherheit

### Schritt 3: CI/CD-Pipeline mit Pre-Releases und Releases

**Beschreibung:** Das GitHub-Repository soll eine CI/CD-Pipeline erhalten, die Build, Tests und
Release-Prozess automatisiert. Verbindliche Grundlage ist die Vorlage
`docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/inventory/external-sources/ci-workflows-instructions.md`
mit allen darin beschriebenen Workflows, gemeinsamen Bausteinen, Versionsermittlung,
Troubleshooting-Hinweisen und der Checkliste. Das Branch-Modell ist `main` (stabile Releases),
`staging` (Pre-Releases) und Entwicklungszweige. Pull Requests nach `main` sind nur von `staging`
aus zulässig.

Die Vorlage ist auf die MAUI-App anzupassen. Die Pipeline baut die Windows- und die iOS-Variante
(iOS 16 als Mindestversion). Sie führt Formatprüfung, statische Analyse, die Prüfung der
Abhängigkeiten auf bekannte Sicherheitslücken sowie Unit-, Integrations- und FlaUI-E2E-Tests
(Windows) aus und erzwingt die Mindest-Testabdeckung der Vorlage. Pushes auf `staging` erzeugen
automatisch Pre-Release-Versionen, Pushes auf `main` finale Releases. Die Windows-Anwendung ist
Release-Artefakt, und zwar als gezipptes Build, das nach dem Entpacken ohne Installation startet.
Ein Windows-Installer (MSIX oder Setup) wird nicht erstellt. Die Versionierung folgt den Regeln der Vorlage: Die erste Version ist 0.1.0, und
bis Version 1.0 darf keine automatische Anhebung auf 1.0 erfolgen.

Geheimnisse und umgebungsspezifische Werte kommen ausschließlich über GitHub Secrets bzw.
Repository-Variablen in die Pipeline, nie aus dem Repository. Dazu gehören die API-Schlüssel für
Kraftstoffpreis- und Routing-Dienst, Signierungsdaten, Bundle-ID und Apple-Team-ID. Solange
Signierungsdaten und Apple-Kennungen fehlen, baut die Pipeline die iOS-Variante unsigniert und
schlägt deswegen nicht fehl. Weil das Repository bis 1.0 privat ist und Actions am Billing-Limit
scheitern können, gibt es einen gleichwertigen lokalen Prüflauf, der dieselben Prüfungen auf dem
Entwicklungsrechner ausführt; seine Nutzung wird dokumentiert. Die einmalig nötigen
Repository-Einstellungen (Branches, Branch-Schutz, Labels, Secrets und Variablen) werden als
Checkliste in der Projektdokumentation festgehalten. Nachgewiesen wird das Ergebnis durch einen
erfolgreichen lokalen Prüflauf und syntaktisch gültige Workflows.

Ergänzung durch Stakeholder-Entscheidung vom 2026-10-04: Für das iOS-Deployment ist die in einem
anderen Projekt bewährte Lösung maßgeblich, die der Anwender unter `drafts/` (Repository-Root,
nicht versioniert, darf nicht committet werden) bereitgestellt hat. Das Skript
`drafts/iOS-Deployment.ps1` wird unter demselben Namen als `scripts/iOS-Deployment.ps1` für
Tankradar übernommen und angepasst (Projektpfade, Umgebungsvariablen mit Präfix `TANKRADAR_IOS_*`,
alle Aktionen build/simulator/device/store/upload/list/menu einschließlich Pair-to-Mac von Windows
und TestFlight-Upload). Die Workflows und gemeinsamen Bausteine werden an `drafts/github/`
angeglichen, insbesondere der iOS-Paketierungsbaustein (`package-ios`: Import von Zertifikat und
Provisioning-Profil in eine temporäre Keychain, Pinning der iOS-Workload auf eine mit Release-Xcode
kompatible Version, Buildnummer aus der Commit-Anzahl, signierter `.ipa`-Build und TestFlight-Upload
per iTMSTransporter, Aufräumen der Keychain) samt Einbindung in Pre-Release- und Release-Workflow,
gesteuert über die Repository-Variable `IOS_SIGNING_ENABLED` und die Secrets `IOS_*` wie in der
Vorlage. Ohne gesetzte Variable bzw. Secrets läuft die Pipeline weiterhin ohne Fehlschlag. Android-
Bestandteile der Vorlage entfallen, da Tankradar kein Android-Ziel hat. Die Einrichtungs-Checkliste
nennt alle auf Apple- und GitHub-Seite nötigen Schritte (App-ID/Bundle-ID, App-Eintrag in App Store
Connect, Provisioning-Profil, Distribution-Zertifikat, App-Store-Connect-API-Key, Secrets und
Variable). Die Bundle-ID liefert der Anwender nach; bis dahin gilt der bestehende Platzhalter.

**Abhängigkeiten:** 1, 2

**Betroffene Bereiche:** Build, Tests, Release-Prozess, Versionierung, CI-Sicherheit

### Schritt 4: Lokale Datenhaltung und Einstellungen

**Beschreibung:** Die App soll alle Daten in einer einzigen lokalen SQLite-Datenbank speichern. Die
Datenbank wird beim ersten Start angelegt und bei künftigen App-Updates ohne Datenverlust auf den
neuen Stand gebracht. Unter iOS ist sie über den systemseitigen Dateischutz (Data Protection)
geschützt; eine zusätzliche Verschlüsselung erfolgt nicht. Als erste Nutzerdaten werden die
Einstellungen dort gespeichert, und der Bereich „Optionen" wird gemäß Designentwurf umgesetzt:

- Auswahl und Reihenfolge der anzuzeigenden Spritsorten
- GPS-Nutzung („Immer", „Nur bei Nutzung", „Nie")
- Standardansicht der Suchergebnisse (Liste oder Karte)
- Standardsortierung (Preis, Entfernung, Name)

Strompreise sind nicht Teil von Version 1.0, daher gibt es keine Einstellung zur
Strompreis-Anzeige und keine Auswahl von Ladetypen. Beim ersten Start gelten sinnvolle
Standardwerte nach dem Grundsatz „sicher statt bequem", z. B. Standort nur bei Nutzung. Änderungen
bleiben nach einem Neustart erhalten. Tests decken das Speichern und Laden der Einstellungen, das
Anlegen und Aktualisieren der Datenbank sowie per FlaUI das Ändern von Einstellungen in der
Windows-App ab. Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 1

**Betroffene Bereiche:** Datenhaltung, Einstellungen

### Schritt 5: Anbindung der Kraftstoffpreis-API und Preis-Cache

**Beschreibung:** Die App soll aktuelle Kraftstoffpreise und Tankstellendaten über die
Tankerkönig-API abrufen, die die offiziellen Daten der Markttransparenzstelle für Kraftstoffe
bereitstellt. Abrufbar sind:

- Tankstellen im Umkreis einer Position, gefiltert nach Spritsorten
- die Details einer einzelnen Tankstelle: Name, Adresse, Preise je Sorte, Öffnungszeiten und
  weitere Angaben, soweit die Quelle sie liefert

Angaben, die die Quelle nicht liefert (z. B. Zahlungsmöglichkeiten), werden nicht erfunden, sondern
später ausgeblendet. Der Hinweis „Automatentankstelle" wird, wo möglich, aus den Öffnungszeiten
(durchgehend geöffnet) abgeleitet.

Die Kommunikation erfolgt ausschließlich über HTTPS, mit Zeitlimit und einer Wiederholungsstrategie
mit zunehmender Wartezeit. Abrufe werden gemäß den Nutzungsbedingungen des Anbieters gedrosselt;
wo möglich, liefert der lokale Preis-Cache die Daten statt eines erneuten Abrufs. Der API-Schlüssel
steht niemals im Quellcode. Er gelangt beim Build aus einem Secret bzw. einer nicht versionierten
lokalen Konfiguration in die App und liegt zur Laufzeit in der sicheren Ablage des
Betriebssystems (iOS: Keychain, Windows: Credential Locker). Die Quellenangabe gemäß Lizenz
CC BY 4.0 („Daten: Tankerkönig / MTS-K") ist in der App sichtbar, z. B. im Bereich „Optionen".

Jeder abgerufene Preis wird mit Zeitstempel in der lokalen Datenbank gespeichert; ältere Preisstände
bleiben für Auswertungen erhalten. Ob ein Preis aktuell ist, ergibt sich ausschließlich aus seinem
Zeitstempel: Ab 60 Minuten Alter gilt er als veraltet, und daraus leitet sich auch der Hinweis
„Preis unbestätigt" ab. Ist keine Verbindung möglich, liefert die App die zuletzt bekannten Preise
samt Alter statt eines Fehlers (Offline-Betrieb). Die App erkennt außerdem, ob eine Netzverbindung
besteht und wann sie wiederhergestellt wird, damit die Ansichten danach veraltete Preise
selbstständig aktualisieren und den Offline-Hinweis entfernen können. Fehlerzustände dürfen nicht
zu unsicherem Verhalten führen. Für Integrations- und E2E-Tests wird ein Mock-Server bzw. eine
Test-API bereitgestellt, sodass kein Test produktive Endpunkte nutzt. Die E2E-Tests lassen sich
per Test-Konfiguration so starten, dass dieser Mock-Dienst nicht erreichbar ist, um den
Offline-Betrieb zu prüfen. Tests decken Abruf, Wiederholungsverhalten, Zeitlimit, Speicherung mit
Zeitstempel, Aktualitätsbewertung (Grenze 60 Minuten), abgeleitete Hinweise, den Offline-Rückfall
und die Erkennung einer wiederhergestellten Verbindung ab. Weil die Quellenangabe im Bereich
„Optionen" sichtbar wird, wird nach Abschluss ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 4

**Betroffene Bereiche:** Preisabruf, Preis-Cache, Offline-Betrieb, Verbindungserkennung, Sicherheit der Zugangsdaten

### Schritt 6: Umkreissuche nach aktuellem Standort mit Ergebnisliste

**Beschreibung:** Im Bereich „Karte" (Suche) sollen Nutzer Tankstellen im Umkreis ihres aktuellen
Standorts finden. Der Radius ist einstellbar (5–50 km), die Ergebnisse lassen sich nach
Spritsorten filtern. Die Ergebnisliste zeigt je Tankstelle:

- Name und Entfernung
- Preise der in den Einstellungen gewählten Spritsorten, in der dort festgelegten Reihenfolge
- Zusatzhinweise wie „Preis unbestätigt" oder „Automatentankstelle", soweit vorhanden

Die Liste ist nach Preis, Entfernung oder Name sortierbar; voreingestellt ist die Standardsortierung
aus den Einstellungen. Das Alter jedes Preises wird als „vor X Min." angezeigt; ab 60 Minuten ist
der Preis in Amber als veraltet markiert. Ohne Verbindung erscheinen die zuletzt bekannten Preise,
und die Kopfzeile zeigt einen Offline-Hinweis.

Der Standort wird nur abgefragt, wenn die Suche ihn benötigt, und nur gemäß der Einstellung zur
GPS-Nutzung. Bei „Nie" findet keine Abfrage statt, und die App weist darauf hin. Standortdaten
werden nicht dauerhaft gespeichert, insbesondere keine GPS-Rohkoordinaten; gespeichert werden
höchstens berechnete Entfernungen oder Tankstellen-IDs. Unter iOS beschreibt der
Berechtigungsdialog den Zweck klar („Ermittlung von Tankstellen in der Nähe"). Radius- und
Filtereingaben werden vor dem API-Aufruf validiert. Die Gestaltung folgt dem Designentwurf. Tests
decken Filterung, Sortierung, Validierung, Standortberechtigung, Altersanzeige und per FlaUI den
Suchablauf mit Testdaten ab. Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 4, 5

**Betroffene Bereiche:** Suche, Standort, Datenschutz, Ergebnisdarstellung

### Schritt 7: Suche nach Adresse, Ort oder PLZ

**Beschreibung:** Neben der Suche am aktuellen Standort soll die Tankstellensuche eine Suche nach
einer eingegebenen Adresse, einem Ort oder einer Postleitzahl bieten. Der Nutzer gibt den
Suchbegriff ein und legt den Radius fest (5–50 km). Die App wandelt die Eingabe über
OpenStreetMap-Nominatim in eine Position um, auf iOS und Windows gleichermaßen. Die Tankstellen im
Umkreis erscheinen in derselben Ergebnisliste wie bei der Standortsuche: Preise der gewählten
Spritsorten mit Altersangabe, Entfernung zur gesuchten Adresse, Zusatzhinweise, Sortierung nach
Preis, Entfernung oder Name sowie Spritsortenfilter.

Die Nutzungsrichtlinie von Nominatim wird eingehalten: Eine Anfrage geht nur bei ausdrücklichem
Absenden der Suche hinaus (keine Autovervollständigung), höchstens eine pro Sekunde, mit einer
Kennung, die die App identifiziert. Die Quellenangabe für OpenStreetMap wird angezeigt. Eingaben
werden validiert, bevor sie an den externen Dienst gehen. Nicht auffindbare Adressen und fehlende
Verbindung werden verständlich gemeldet. Eingegebene Adressen und daraus ermittelte Positionen
werden nicht dauerhaft gespeichert. Die Adresssuche braucht kein GPS und funktioniert auch bei der
Einstellung „GPS-Nutzung: Nie". Tests decken Validierung, Adressauflösung gegen einen
Test-/Mock-Dienst, Einhaltung der Anfragebegrenzung und per FlaUI den Ablauf der Adresssuche ab.
Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 6

**Betroffene Bereiche:** Suche, Adressauflösung, Eingabevalidierung

### Schritt 8: Tankstellen-Detailansicht

**Beschreibung:** Tippt der Nutzer eine Tankstelle in der Suchergebnisliste an, soll sich eine
vollständige Detailansicht gemäß Designentwurf öffnen. Sie zeigt:

- Name, Adresse und Entfernung
- die Preise der in den Einstellungen aktivierten Spritsorten, jeweils mit Altersangabe „vor X Min."
  und Amber-Markierung ab 60 Minuten
- Zusatzhinweise wie „Preis unbestätigt" oder „Automatentankstelle"
- Öffnungszeiten
- Zahlungsmöglichkeiten, sofern die Datenquelle sie liefert

Angaben, die die Datenquelle (Tankerkönig) nicht liefert, werden weggelassen statt als leere Felder
angezeigt. Strompreise und Ladeinformationen sind nicht Teil von Version 1.0 und erscheinen nicht.
Ohne Verbindung zeigt die Ansicht die zuletzt bekannten Daten mit Hinweis auf ihr Alter und einen
Offline-Hinweis in der Kopfzeile. Mit Verbindung lassen sich die Preise aktualisieren. Wird die
Netzverbindung wiederhergestellt, während die Detailansicht geöffnet ist, aktualisiert die App
veraltete Preise automatisch und entfernt den Offline-Hinweis. Tests decken die Aufbereitung der
Detaildaten (inkl. ausgeblendeter fehlender Angaben), die automatische Aktualisierung nach
Wiederverbindung und per FlaUI das Öffnen der Detailansicht aus der Ergebnisliste ab. Nach Abschluss wird ein startfähiger
Windows-Zwischenstand unter `review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt
(nicht committen).

**Abhängigkeiten:** 6

**Betroffene Bereiche:** Tankstellen-Details, Preisanzeige

### Schritt 9: Kartenansicht der Suchergebnisse

**Beschreibung:** Die Ergebnisse der Tankstellensuche (Standort- und Adresssuche) sollen
alternativ zur Liste auf einer Karte erscheinen. Der Nutzer kann zwischen Liste und Karte wechseln;
voreingestellt ist die Standardansicht aus den Einstellungen. Die Karte ist eine
plattformübergreifende Kartendarstellung auf Basis von OpenStreetMap-Kacheln. Sie funktioniert unter
iOS und Windows gleich und ist dadurch auch in den FlaUI-Tests prüfbar. Die Quellenangabe
„© OpenStreetMap-Mitwirkende" ist sichtbar. Die Karte lässt sich zoomen und verschieben. Bei der
Standortsuche ist der eigene Standort markiert, bei der Adresssuche die gesuchte Position; der
Standort wird dafür nur gemäß der Einstellung zur GPS-Nutzung abgefragt und nicht gespeichert.

Jede Tankstelle erscheint als Markierung mit Preisangabe. Die Farbe richtet sich nach dem
Preisniveau innerhalb der aktuellen Ergebnismenge:

- Grün: günstigster Preis
- Rot: Preise im oberen Drittel der Spanne zwischen niedrigstem und höchstem Preis
- Teal: alle übrigen Preise
- Grau: geschlossene Stationen

Maßgeblich ist der Preis der aktuell gefilterten Spritsorte, ohne Filter der zuerst gewählten Sorte
aus den Einstellungen. Tippt der Nutzer eine Markierung an, öffnet sich die
Tankstellen-Detailansicht. Tests decken die Einstufung der Preisniveaus (inkl. Grenzfälle wie
gleiche Preise und einzelne Station) und per FlaUI den Wechsel zwischen Liste und Karte, Zoomen und
Verschieben sowie das Öffnen der Details über die Karte ab. Kartenkacheln kommen in Tests nicht von produktiven Servern.
Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 6, 8

**Betroffene Bereiche:** Suche, Kartendarstellung

### Schritt 10: Favoritengruppen und Zuordnung von Tankstellen

**Beschreibung:** Nutzer sollen favorisierte Tankstellen in selbst benannten Gruppen verwalten
(z. B. „Arbeitsweg", „Heimat", „Urlaub"). Eine Tankstelle darf mehreren Gruppen angehören. In der
Tankstellen-Detailansicht gibt es die Schaltfläche „Zu Favoriten hinzufügen". Sie bietet die
Auswahl einer bestehenden Gruppe oder das Anlegen einer neuen an. Ist die Tankstelle bereits einer
oder mehreren Gruppen zugeordnet, gibt es zusätzlich „Aus Favoriten entfernen"; bei mehreren Gruppen
wählt der Nutzer dort, aus welcher Gruppe oder welchen Gruppen sie entfernt wird. Tankstellen werden
ausschließlich über die Detailansicht Gruppen zugeordnet.

Gruppen können erstellt, umbenannt und (nach Rückfrage) gelöscht werden. Zu jeder Gruppe lässt
sich optional eine kurze Beschreibung hinterlegen und ändern. Die Gruppenansicht listet
die Tankstellen einer Gruppe. Zu jedem Eintrag lassen sich optional eine Notiz und eine Priorität
pflegen; nach der Priorität ist die Liste geordnet. Favoriten und Gruppen werden lokal gespeichert
und stehen offline zur Verfügung. Tests decken die Gruppenoperationen, die Mehrfachzuordnung, das
Hinzufügen und Entfernen sowie per FlaUI den Ablauf „Tankstelle suchen – Details öffnen – zu neuer
Gruppe hinzufügen – Gruppe umbenennen – entfernen" ab. Nach Abschluss wird ein startfähiger
Windows-Zwischenstand unter `review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt
(nicht committen).

**Abhängigkeiten:** 8

**Betroffene Bereiche:** Favoriten, Gruppenverwaltung, Tankstellen-Details

### Schritt 11: Startseite mit Favoritengruppen und Bereich „In der Nähe"

**Beschreibung:** Die Startseite (Bereich „Favoriten") soll gemäß Designentwurf alle
Favoritengruppen mit ihren Tankstellen anzeigen. Zu jeder Tankstelle erscheinen:

- Name
- Entfernung, sofern ein Standort ermittelt werden darf
- die Preise der in den Einstellungen gewählten Spritsorten in der dort festgelegten Reihenfolge,
  mit Altersangabe „vor X Min." und Amber-Markierung ab 60 Minuten
- Zusatzhinweise wie „Preis unbestätigt" oder „Automatentankstelle"

Beim Öffnen und auf Wunsch des Nutzers werden die Preise aktualisiert. Ohne Verbindung erscheinen
die zuletzt bekannten Preise mit Altersangabe und einem Offline-Hinweis in der Kopfzeile. Wird die
Netzverbindung wiederhergestellt, während die Startseite angezeigt wird, aktualisiert die App
veraltete Preise automatisch und entfernt den Offline-Hinweis. Tippt der
Nutzer eine Tankstelle an, öffnet sich ihre Detailansicht.

Zusätzlich gibt es einen optional aktivierbaren Bereich „In der Nähe". Er erscheint nur, wenn er in
den Einstellungen aktiviert ist und die GPS-Nutzung es erlaubt. Er zeigt Tankstellen um den
aktuellen Standort, ist nach Spritsorten filterbar und dient ausschließlich der Information: Direkt
aus diesem Bereich gibt es keine Favoritenaktionen. Der Schalter zur Aktivierung dieses Bereichs
wird in den Einstellungen ergänzt. Standortdaten werden nicht gespeichert. Gibt es noch keine
Gruppen, zeigt die Startseite einen Hinweis, wie man Favoriten anlegt. Tests decken die Aufbereitung
der Gruppenübersicht, die automatische Aktualisierung nach Wiederverbindung und per FlaUI die Anzeige von Gruppen, Preisen und des Bereichs „In der Nähe"
mit Testdaten ab. Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 5, 6, 10

**Betroffene Bereiche:** Startseite, Favoriten, Standort, Preisanzeige, Einstellungen

### Schritt 12: Fahrzeugverwaltung im Tankbuch

**Beschreibung:** Im Bereich „Tankbuch" sollen Nutzer mehrere Fahrzeuge verwalten. Zu jedem
Fahrzeug werden erfasst:

- Name
- Kraftstoffart (z. B. Benzin, Diesel, Elektro)
- Start-Kilometerstand
- optional ein Startverbrauch

Fahrzeuge können angelegt, bearbeitet und gelöscht werden. Hat ein Fahrzeug bereits Tank- oder
Ladevorgänge, verlangt das Löschen eine ausdrückliche Rückfrage. Sie sagt klar, dass die
zugehörigen Vorgänge mitgelöscht werden. Eingaben werden validiert (z. B. kein negativer Kilometerstand). Die Daten liegen
ausschließlich lokal, sind vollständig offline nutzbar und werden nicht an Dritte weitergegeben. Die
Gestaltung folgt dem Designentwurf (Eingabefelder mit Einheiten). Tests decken Validierung und
Verwaltung, das Mitlöschen der Vorgänge sowie per FlaUI das Anlegen, Bearbeiten und Löschen eines
Fahrzeugs ab. Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 4

**Betroffene Bereiche:** Tankbuch, Fahrzeuge, Datenhaltung

### Schritt 13: Tank- und Ladevorgänge erfassen

**Beschreibung:** Im Tankbuch sollen Nutzer Tank- und Ladevorgänge für ein Fahrzeug erfassen,
einsehen, korrigieren und löschen. Ein Vorgang enthält:

- Datum und Uhrzeit
- das Fahrzeug
- die Tankstelle: Auswahl aus einer Liste bekannter Tankstellen (z. B. aus den Favoriten) oder
  manuelle Eingabe eines Namens
- die Art (Kraftstoff oder Strom) mit Menge in Litern bzw. kWh
- Preis pro Einheit und Gesamtbetrag
- den Kilometerstand
- ein Kennzeichen „Teilbetankung"
- optional eine Notiz und ein Foto des Belegs

Alle Werte gibt der Nutzer selbst ein. Ladevorgänge brauchen keine Strompreisquelle; Ladeorte gibt
der Nutzer wie jede andere Station manuell als Namen ein.

Menge, Preis pro Einheit und Gesamtbetrag werden auf Plausibilität geprüft: Aus zwei Werten ergibt
sich der dritte, Abweichungen werden gemeldet. Der Kilometerstand darf nicht unter dem vorherigen
Eintrag bzw. dem Start-Kilometerstand liegen. Zu jedem vollen Tank- bzw. Ladevorgang zeigt das
Tankbuch den Verbrauch pro 100 km als (Menge / seit dem vorherigen Vollvorgang gefahrene Strecke) ×
100. Standardmäßig gilt jeder Vorgang als Volltankung bzw. volle Ladung. Ist ein Vorgang als
Teilbetankung gekennzeichnet, bekommt er keinen eigenen Verbrauchswert; seine Menge wird dem nächsten
Vollvorgang zugerechnet.

Die Einträge eines Fahrzeugs erscheinen chronologisch. Das Tankbuch ist vollständig offline
nutzbar. Belegfotos bleiben lokal auf dem Gerät und sind über den systemseitigen Dateischutz
geschützt. Tankbuchdaten werden nicht an Dritte weitergegeben. Tests decken Validierung und
Verbrauchsberechnung ab, einschließlich Randfällen wie erster Eintrag, Teilbetankungen und
Ladevorgängen in kWh, und per FlaUI das Erfassen eines Tankvorgangs mit anschließender
Verbrauchsanzeige.

Zusätzlich prüft ein FlaUI-Szenario den Offline-Modus: Die App startet mit Testdaten, und der
Mock-Dienst für Kraftstoffpreise ist nicht erreichbar. Geprüft wird, dass Startseite und
Tankstellen-Detailansicht die zwischengespeicherten Preise mit Altersangabe und Offline-Hinweis
zeigen und dass das Tankbuch trotzdem voll nutzbar ist: Fahrzeug anlegen, Vorgang erfassen,
Verbrauch ansehen, Vorgang korrigieren und löschen. Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 10, 11, 12

**Betroffene Bereiche:** Tankbuch, Tankvorgänge, Verbrauchsberechnung, Belegfotos, Offline-Betrieb

### Schritt 14: Verbrauchs- und Kostenauswertung

**Beschreibung:** Das Tankbuch soll je Fahrzeug historische Statistiken gemäß Designentwurf
anzeigen:

- Durchschnittsverbrauch (l/100 km bzw. kWh/100 km bei Elektrofahrzeugen)
- Kosten pro 100 km
- Monatskosten
- grafische Verläufe von Verbrauch und Kosten über die Zeit

Grundlage sind die erfassten Tank- und Ladevorgänge. Teilbetankungen fließen mit ihrer Menge in
den nächsten Vollvorgang ein und mit ihren Kosten in die Kostenwerte. Liegen noch zu wenige Vorgänge
vor, zieht die Auswertung den optional hinterlegten Startverbrauch des Fahrzeugs heran oder zeigt
einen verständlichen Hinweis. Die Auswertung ist vollständig offline verfügbar und aktualisiert sich
nach jedem Erfassen, Korrigieren oder Löschen eines Vorgangs. Tests decken die Berechnungen mit
Beispieldaten ab, einschließlich Monatsgrenzen, Teilbetankungen und Fahrzeugen ohne Einträge, und
per FlaUI die Anzeige der Kennzahlen nach dem Erfassen von Vorgängen. Nach Abschluss wird ein
startfähiger Windows-Zwischenstand unter `review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root
abgelegt (nicht committen).

**Abhängigkeiten:** 13

**Betroffene Bereiche:** Tankbuch, Statistik, Diagramme

### Schritt 15: Lokale Sicherung und Wiederherstellung

**Beschreibung:** Im Bereich „Optionen" soll der Nutzer eine lokale Sicherung erstellen und aus
einer Sicherung wiederherstellen können. Die Sicherung enthält alle Nutzerdaten (Favoriten, Gruppen
mit Notizen und Prioritäten, Fahrzeuge, Tankbuch mit Belegfotos, Einstellungen) sowie alle
gespeicherten Kraftstoffpreisstände mit Zeitstempel. Nach einer Wiederherstellung stehen die Daten
vollständig zur Verfügung, und historische Daten bleiben für Auswertungen erhalten. Veraltete
Preisstände erkennt die App an den Zeitstempeln und aktualisiert sie bei Bedarf über die API.
Sicherungen älterer App-Versionen lassen sich einspielen.

Erstellte Sicherungen legt die App lokal auf dem Gerät ab. Der Bereich „Optionen" listet die
vorhandenen Sicherungen mit Erstellungsdatum und Größe. Der Nutzer kann eine Sicherung aus der Liste
zur Wiederherstellung auswählen und einzelne Sicherungen nach Rückfrage löschen. Zusätzlich lässt
sich eine Sicherung über die Teilen-/Speichern-Funktion des Betriebssystems weitergeben, und eine
außerhalb der App gespeicherte Sicherungsdatei lässt sich einspielen.

Bevor vorhandene Daten überschrieben werden, fragt die App nach. Eine beschädigte oder ungültige
Sicherungsdatei führt zu einer verständlichen Fehlermeldung, ohne die vorhandenen Daten zu
verändern. API-Zugangsdaten gehören nicht zur Sicherung. Sicherungen entstehen ausschließlich auf
Veranlassung des Nutzers und werden nicht automatisch an Dritte oder eine Cloud übertragen.

Außerdem wird die Rückfrage beim Löschen eines Fahrzeugs mit Tank- oder Ladevorgängen im Tankbuch
ergänzt: Sie weist nun darauf hin, dass sich vorher eine Sicherung erstellen lässt.

Tests decken Sicherung und Wiederherstellung (Vollständigkeit, Zeitstempel, ungültige Dateien,
Sicherung einer älteren Version), die Liste der Sicherungen und das Löschen einzelner Sicherungen
ab, per FlaUI außerdem den Ablauf „Sicherung erstellen – Daten ändern – Sicherung aus der Liste
wiederherstellen – Sicherung löschen". Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 11, 14

**Betroffene Bereiche:** Sicherung, Wiederherstellung, Sicherungsverwaltung, Datenhaltung, Einstellungen, Fahrzeuge

### Schritt 16: Export des Tankbuchs als CSV oder PDF

**Beschreibung:** Nutzer sollen die Tank- und Ladevorgänge eines Fahrzeugs aus dem Tankbuch
exportieren können, wahlweise eingeschränkt auf einen Zeitraum. Zur Wahl stehen:

- eine CSV-Datei zur Weiterverarbeitung in einer Tabellenkalkulation
- ein PDF-Bericht mit den Einträgen und den Kennzahlen (Durchschnittsverbrauch, Kosten pro 100 km,
  Monatskosten)

Ein Export entsteht ausschließlich auf Veranlassung des Nutzers und wird über die
Teilen-/Speichern-Funktion des Betriebssystems bereitgestellt; es gibt keine automatische
Weitergabe an Dritte. Die CSV-Datei muss in deutschsprachigen Tabellenkalkulationen korrekt lesbar
sein (Umlaute, Dezimaltrennzeichen, Einheiten Liter bzw. kWh). Tests decken Inhalt und Format beider
Exporte ab, einschließlich Zeitraumfilter und Fahrzeugen ohne Einträge, und per FlaUI das Auslösen
eines Exports. Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 14

**Betroffene Bereiche:** Tankbuch, Export

### Schritt 17: Tankstellen entlang einer Route

**Beschreibung:** Die Tankstellensuche im Bereich „Karte" soll zusätzlich zu Standort- und
Adresssuche eine Suche entlang einer Route anbieten. Der Nutzer gibt Start und Ziel als Adresse, Ort
oder Postleitzahl ein. Als Start kann er auch den aktuellen Standort wählen, sofern die Einstellung
zur GPS-Nutzung das erlaubt. Start und Ziel werden über dieselbe Adressauflösung wie bei der
Adresssuche (OpenStreetMap-Nominatim) in Positionen umgewandelt. Die Route für ein Auto ermittelt der
Routing-Dienst OpenRouteService. Die App zeigt die Tankstellen innerhalb eines einstellbaren Abstands
zur Route und kombiniert dafür die Umkreisabfragen der Kraftstoffpreis-API entlang der Strecke. Die
Abfragen werden so bemessen, dass die Nutzungsbedingungen des Preisanbieters eingehalten werden.

Die Ergebnisse erscheinen in Liste und Karte mit den gewohnten Spritsortenfiltern, Preisen mit
Altersangabe, Zusatzhinweisen, Preisniveau-Farben und Sortierungen. Die Sortierung nach Entfernung
bezieht sich hier auf die Position entlang der Route vom Start aus. Die Route wird auf der Karte
eingezeichnet. Tippt der Nutzer eine Tankstelle an, öffnet sich ihre Detailansicht.

Die Kommunikation mit dem Routing-Dienst erfolgt ausschließlich über HTTPS, mit Zeitlimit und einer
Wiederholungsstrategie mit zunehmender Wartezeit. Der API-Schlüssel für OpenRouteService steht
niemals im Quellcode. Er gelangt beim Build aus einem Secret bzw. einer nicht versionierten lokalen
Konfiguration in die App und liegt zur Laufzeit in der sicheren Ablage des Betriebssystems (iOS:
Keychain, Windows: Credential Locker). Die Quellenangabe für OpenRouteService und OpenStreetMap wird
angezeigt. Eingaben werden validiert, bevor sie an externe Dienste gehen. Nicht auffindbare Orte,
nicht berechenbare Routen und fehlende Verbindung werden verständlich gemeldet. Routen, eingegebene
Adressen und Standortdaten werden nicht dauerhaft gespeichert. Tests nutzen Test- bzw. Mock-Dienste
für Routing, Adressauflösung und Preise. Sie decken die Auswahl der Tankstellen entlang der Route
(Abstand, Reihenfolge), die Validierung, das Fehlerverhalten und per FlaUI den Suchablauf ab. Nach
Abschluss wird ein startfähiger Windows-Zwischenstand unter `review-versions/<Version>_<JJJJ-MM-TT>/`
im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 7, 9

**Betroffene Bereiche:** Suche, Routenermittlung, Adressauflösung, Kartendarstellung, Sicherheit der Zugangsdaten

### Schritt 18: iOS-Build, Signierung und TestFlight-Auslieferung

**Beschreibung:** Die iOS-Variante von Tankradar soll reproduzierbar gebaut, signiert und über
TestFlight bzw. den App Store ausgeliefert werden können. Das Deployment-Skript
`scripts/iOS-Deployment.ps1` und der signierte iOS-Build samt TestFlight-Upload in der CI-Pipeline
sind bereits mit Schritt 3 entstanden (Stakeholder-Entscheidung vom 2026-10-04, Vorlage unter
`drafts/`). Dieser Schritt prüft und vervollständigt sie für die fertige App: Aktionen für Build,
Simulator, Gerät, Store-Upload mit automatischer Erhöhung der Buildnummer und den Upload vorhandener
Pakete, nutzbar von Windows aus über einen gekoppelten Mac. Mindestversion ist iOS 16.

Bundle-ID, Apple-Team-ID, Signierungsdaten und App-Store-Connect-Zugangsdaten liegen ausschließlich
außerhalb des Repositorys (lokale Umgebungsvariablen bzw. GitHub Secrets und Variablen). Der
Anwender liefert diese Werte nach. Bis dahin erkennt das Skript fehlende Werte und meldet sie
verständlich, ohne andere Aktionen wie Simulator-Builds zu blockieren. Die App fordert unter iOS nur
die tatsächlich benötigten Rechte an: Standortzugriff mit dem Berechtigungstext sinngemäß
„Ermittlung von Tankstellen in der Nähe" sowie Kamera- bzw. Fotozugriff, begründet ausschließlich
für Belegfotos. Datenbank und Belegfotos sind über den systemseitigen Dateischutz (Data Protection)
geschützt. Sind Signierungsdaten hinterlegt, erzeugt die CI-Pipeline bei Releases einen signierten
iOS-Build.

Die für die App-Store-Prüfung nötigen Datenschutzangaben werden in der Projektdokumentation
vorbereitet: Standortnutzung, Weitergabe von Adress- und Routeneingaben an Geokodierungs- und
Routing-Dienst, keine Weitergabe von Fahrzeug- und Tankbuchdaten. Hilfsfunktionen des Skripts, die
ohne Mac lauffähig sind, werden automatisiert getestet. Die Nutzung des Skripts wird dokumentiert.

**Abhängigkeiten:** 3, 13, 17

**Betroffene Bereiche:** iOS-Auslieferung, Signierung, Datenschutzangaben, CI

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|-----------------|----------------------|
| 1 | App-Grundgerüst, Navigation und Design-System | Keine | Projektstruktur, Navigation, Design-System, Testinfrastruktur, Ablage von Zwischenständen |
| 2 | Lokale Git-Hooks zur Qualitätssicherung | 1 | Entwicklungsprozess, Codequalität, Repository-Sicherheit |
| 3 | CI/CD-Pipeline mit Pre-Releases und Releases | 1, 2 | Build, Tests, Release-Prozess, Versionierung, CI-Sicherheit |
| 4 | Lokale Datenhaltung und Einstellungen | 1 | Datenhaltung, Einstellungen |
| 5 | Anbindung der Kraftstoffpreis-API und Preis-Cache | 4 | Preisabruf, Preis-Cache, Offline-Betrieb, Verbindungserkennung, Sicherheit der Zugangsdaten |
| 6 | Umkreissuche nach aktuellem Standort mit Ergebnisliste | 4, 5 | Suche, Standort, Datenschutz, Ergebnisdarstellung |
| 7 | Suche nach Adresse, Ort oder PLZ | 6 | Suche, Adressauflösung, Eingabevalidierung |
| 8 | Tankstellen-Detailansicht | 6 | Tankstellen-Details, Preisanzeige |
| 9 | Kartenansicht der Suchergebnisse | 6, 7, 8 | Suche, Kartendarstellung |
| 10 | Favoritengruppen und Zuordnung von Tankstellen | 8 | Favoriten, Gruppenverwaltung, Tankstellen-Details |
| 11 | Startseite mit Favoritengruppen und Bereich „In der Nähe" | 5, 6, 10 | Startseite, Favoriten, Standort, Preisanzeige, Einstellungen |
| 12 | Fahrzeugverwaltung im Tankbuch | 4 | Tankbuch, Fahrzeuge, Datenhaltung |
| 13 | Tank- und Ladevorgänge erfassen | 10, 11, 12 | Tankbuch, Tankvorgänge, Verbrauchsberechnung, Belegfotos, Offline-Betrieb |
| 14 | Verbrauchs- und Kostenauswertung | 13 | Tankbuch, Statistik, Diagramme |
| 15 | Lokale Sicherung und Wiederherstellung | 11, 14 | Sicherung, Wiederherstellung, Sicherungsverwaltung, Datenhaltung, Einstellungen, Fahrzeuge |
| 16 | Export des Tankbuchs als CSV oder PDF | 14 | Tankbuch, Export |
| 17 | Tankstellen entlang einer Route | 7, 9 | Suche, Routenermittlung, Adressauflösung, Kartendarstellung, Sicherheit der Zugangsdaten |
| 18 | iOS-Build, Signierung und TestFlight-Auslieferung | 3, 13, 17 | iOS-Auslieferung, Signierung, Datenschutzangaben, CI |

## Offene Punkte

Keine.

Alle 14 offenen Punkte des vorherigen Plans hat der Stakeholder am 2026-09-28 entschieden:

- Punkt 2: Strompreise entfallen in Version 1.0.
- Punkt 14: Export und Route gehören zu Version 1.0; als Routing-Dienst ist OpenRouteService
  festgelegt.
- Punkte 1 und 3–13: Die Empfehlungen des vorherigen Plans gelten als Entscheidungen.

Die Entscheidungen stehen in der Tabelle der Vorgehensentscheidungen und sind in die
Entwicklungsschritte eingearbeitet. Bundle-ID, Team-ID und die API-Schlüssel (Tankerkönig,
OpenRouteService) liefert bzw. beantragt der Anwender. Sie sind als Konfiguration bzw. Secrets
vorgesehen und blockieren die Umsetzung nicht: Tests laufen gegen Mock-Dienste, und Signierung und
Store-Upload melden fehlende Werte verständlich.

Die sechs Lücken aus der Projektplan-Gegenprüfung hat der Projektleiter am 2026-09-28 geklärt; sie
sind eingearbeitet:

- Aktualisierung bei Wiederverbindung: Schritte 5, 8 und 11.
- Sicherungsverlauf (Liste, Auswahl zur Wiederherstellung, Löschen): Schritt 15.
- Feature-Flags: kein eigener Mechanismus (Vorgehensentscheidung).
- E2E-Szenario Offline-Modus: Schritt 13.
- Windows-Installer: entfällt (Vorgehensentscheidung, Schritt 3).
- Sicherungshinweis in der Löschrückfrage für Fahrzeuge: erst in Schritt 15, nicht in Schritt 12.
