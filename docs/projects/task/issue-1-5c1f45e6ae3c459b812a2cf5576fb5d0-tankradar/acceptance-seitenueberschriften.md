# Abnahme Korrektur – Doppelte Seitenüberschriften entfernt (Version 0.1.27)

- **Datum:** 2026-10-25
- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--seitenueberschriften` (HEAD `9db828c`)
- **Geprüfter Umfang:** `git diff 6540aa0..HEAD` (Commit `9db828c`, 22 Dateien: 5 Seiten-XAML, 13 E2E-Dateien, 1 neuer Unit-Test, `changes.log`, 2 Hilfedateien)
- **Maßstab:** Anwenderwunsch „Die Ansichten in der App bereinigen um überflüssige Überschriften. Auf der Seite ‚Favoriten' steht der Begriff ‚Favoriten' ganz oben auf der Seite. Zusätzlich steht er auch im App-Kopf als Titel. Diese Redundanz soll entfernt werden. Das gleiche gilt für die anderen Ansichten.“
- **Hinweis:** Plan- und Code-Review als Unteragenten wurden bei der Umsetzung ausgelassen. Diese Abnahme ersetzt sie und prüft deshalb Code, Tests und Doku vollständig selbst.

## Gesamturteil

**Abgenommen mit Hinweisen**

Der Anwenderwunsch ist erfüllt. Auf allen fünf Seiten, auf denen der Seitentitel oben im Inhalt wiederholt wurde (Favoriten, Karte, Tankbuch, Optionen, Tank- und Ladevorgänge), ist die Überschrift entfernt. Der Titel steht dort nur noch in der Navigationsleiste. Auf den beiden übrigen Seiten (Stationsdetails, Favoritengruppe) ist die Überschrift keine Wiederholung des Titels, sondern der Name des angezeigten Objekts. Dass sie bleibt, ist richtig. Weitere Ansichten mit eigenem Titel gibt es nicht. Auf iOS bleibt der Titel sichtbar, weil an der Navigationsleiste nichts geändert wurde und das Projekt sie nirgends ausblendet. Unter Windows zeigt die Shell ihn in der Kopfzeile; das belegen die E2E-Tests an der laufenden App.

Selbst ausgeführt und grün:
- Release-Build mit Warnungen als Fehlern (Windows und `net10.0-ios`), auch ohne inkrementellen Build
- Formatprüfung (beide Varianten)
- alle Unit-Tests (1938)
- 42 gezielte E2E-Tests off-screen
- die fünf geforderten Pre-Push-Prüfungen

Es gibt keine kritischen und keine hohen Befunde. Ein mittlerer Befund betrifft die Aussagekraft des neuen E2E-Prüfschritts:

- **M1:** Der Navigationstest zählt die Titeltexte, sobald die Kopfzeile den neuen Titel zeigt. Ob der Seiteninhalt dann schon aufgebaut ist, prüft er nicht. Eine zurückgekehrte Überschrift könnte deshalb unbemerkt bleiben.

Die übrigen Befunde sind niedrig (N1–N6). Ich empfehle, M1, N1, N2, N3 und N5 in einem kurzen Folge-Commit vor dem PR nach `staging` zu erledigen. N4 und N6 sind eigene, optionale Schritte bzw. Entscheidungen des Anwenders.

## Seitenübersicht

Die App hat sieben Seiten (`Views/*Page.xaml`), alle mit `Title` für die Navigationsleiste der Shell. Es gibt keine modalen Seiten, keine Popups und keine `DisplayAlert`/`DisplayActionSheet`-Dialoge. Die beiden `ContentView`s (`StationTile`, `StationMapView`) tragen keine Seitenüberschrift. Eine eigene Hilfe- oder Info-Seite gibt es in der App nicht. Sicherung, Routensuche, Datenquelle (Optionen), Export und Auswertung (Vorgangsseite) sowie die Fahrzeugverwaltung (Formular im Tankbuch) sind Karten bzw. Bereiche innerhalb dieser Seiten, keine eigenen Ansichten. Die Startseite ist die Seite „Favoriten“.

| Seite (Datei) | Titel in der Navigationsleiste | Frühere Überschrift oben im Inhalt | Jetzt | Bewertung |
|---|---|---|---|---|
| Favoriten (`FavoritesPage.xaml`), Startseite | „Favoriten“ (Literal) | „Favoriten“, `headline-lg`, `FavoritesPage.Headline`, ohne `HeadingLevel` | entfernt; Inhalt beginnt mit Offline-Hinweis (nur offline) bzw. „Preise aktualisieren“ | erfüllt |
| Karte (`MapPage.xaml`), inkl. Umkreis-, Adress- und Routensuche | „Karte“ (Literal) | „Karte“, `headline-lg`, `MapPage.Headline`, ohne `HeadingLevel` | entfernt; Inhalt beginnt mit der Karte „Ansicht“ | erfüllt |
| Tankbuch (`TankbookPage.xaml`), inkl. Fahrzeugverwaltung | „Tankbuch“ (`TankbookTexts.PageTitle`) | „Tankbuch“, `headline-lg`, Ebene 1, `TankbookPage.Headline` | entfernt; Inhalt beginnt mit „Deine Fahrzeuge. Alle Angaben bleiben auf diesem Gerät.“, danach Abschnitt „Fahrzeuge“ (Ebene 2) | erfüllt, Hinweise N3, N6 |
| Optionen (`SettingsPage.xaml`), inkl. Routensuche, Sicherung, Datenquelle | „Optionen“ (Literal) | „Optionen“, `headline-lg`, `SettingsPage.Headline`, ohne `HeadingLevel` | entfernt; erstes Element ist jetzt der unsichtbare Text „Einstellungen geladen“ | erfüllt, Hinweis N2 |
| Tank- und Ladevorgänge (`TankbookEntriesPage.xaml`), inkl. Auswertung und Export | „Tank- und Ladevorgänge“ (`TankbookEntryTexts.PageTitle`) | „Tank- und Ladevorgänge“, `headline-lg`, Ebene 1, `TankbookEntriesPage.Headline`; darunter Fahrzeugname Ebene 2 | entfernt; Fahrzeugname (`headline-md`) ist jetzt Ebene 1, Hierarchie 1 → 2 (Auswertung, Export, Vorgänge) → 3 → 4 lückenlos | erfüllt |
| Stationsdetails (`StationDetailPage.xaml`) | „Stationsdetails“ (`DetailTexts.PageTitle`) | Stationsname (`headline-lg`, `Detail.Name`, ohne `HeadingLevel`) | unverändert | vertretbar, Hinweis N4 |
| Favoritengruppe (`FavoriteGroupPage.xaml`) | „Favoritengruppe“ (`FavoritesTexts.GroupPageTitle`) | Gruppenname (`headline-lg`, `FavoriteGroup.Name`, ohne `HeadingLevel`) | unverändert | vertretbar, Hinweis N4 |
| Shell (`AppShell.xaml`) | Fenstertitel „Tankatlas“ (Windows), `Shell.Title` folgt dem Seitentitel | – | unverändert | erfüllt |

Sinngemäße Dopplungen zwischen Titel und Inhalt habe ich nicht gefunden. „Optionen“ (Titel und Reiter) steht nirgends sichtbar als „Einstellungen“ im Inhalt; einzig der unsichtbare Prüftext „Einstellungen geladen“ (siehe N2). Auf der Vorgangsseite ist „Vorgänge, neueste zuerst“ eine Abschnittsüberschrift über der Liste unterhalb von Auswertung und Export und kein Seitenkopf. Eine inhaltliche Nähe besteht nur innerhalb des Tankbuch-Inhalts zwischen Einleitung „Deine Fahrzeuge. …“ und Abschnitt „Fahrzeuge“ (N6, optional).

### Detailansicht und Favoritengruppe: behalten oder dynamischer Titel?

Die Entscheidung, die Überschrift dort zu behalten, ist vertretbar und aus meiner Sicht die bessere Wahl:

- Titel und Überschrift sind dort nicht redundant. Der Titel benennt die Art der Seite, die Überschrift das konkrete Objekt.
- Ein dynamischer Titel (Stationsname bzw. Gruppenname in der Navigationsleiste) würde die Überschrift im Inhalt erst redundant machen. Lange Namen würden in der iOS-Navigationsleiste einzeilig gekürzt, im Inhalt sind 2 bzw. 3 Zeilen möglich. Außerdem ändert sich der Gruppenname beim Bearbeiten, und der Stationsname kann ein eigener Anzeigename sein, der auf der Seite selbst geändert wird; der Titel müsste dann nachgeführt werden.
- Der Name steht jeweils in der Kopfkarte mit den zugehörigen Aktionen (Bearbeiten, Löschen, Status-Chips). Das ist inhaltlich richtig platziert.

## iOS und Sprachausgabe

- **Sichtbarkeit des Titels auf iOS:** Im Projekt gibt es kein `Shell.NavBarIsVisible`, kein `Shell.TitleView`, kein `NavigationPage.HasNavigationBar`, keine Large-Title-Einstellung (`ios:Page.LargeTitleDisplay`/`PrefersLargeTitles`) und keine eigenen Shell-Renderer oder Handler-Mappings für die Navigationsleiste (Suche über `src/Tankradar.MAUI`, `*.xaml`/`*.cs`). Damit gilt der Shell-Standard: Jede Seite der vier Reiter und jede per Route geöffnete Seite (Detail, Favoritengruppe, Vorgänge) zeigt die Navigationsleiste mit `Page.Title` in normaler Größe. `ShellContent.Title` und `Page.Title` sind bei den vier Reitern identisch. Das entspricht auch der Anwenderbeobachtung („steht auch im App-Kopf als Titel“). Ein Gerätetest steht aus, ist für diese Änderung aber nicht nötig, weil an der Navigationsleiste nichts geändert wurde.
- **Gestaltung:** Die Navigationsleiste nutzt Systemschrift und Systemfarben (keine `Shell.TitleColor`/`Shell.BackgroundColor`). Die entfernten Überschriften waren Inter Semibold 24 px. Das ist ein bewusster Wechsel auf den Systemstandard und kein Fehler.
- **VoiceOver:** Der Titel der iOS-Navigationsleiste wird von VoiceOver als Überschrift angesagt und ist das erste Element der Seite. Nach dem Öffnen einer Unterseite (Detail, Favoritengruppe, Vorgänge) setzt iOS den Fokus in die Navigationsleiste. Beim Reiterwechsel bleibt der Fokus wie bisher auf dem Reiter, der den Seitennamen ohnehin nennt. Die Seite wird also benannt.
- **Überschriften-Hierarchie:** iOS kennt keine Überschriftenebenen; MAUI bildet jede `HeadingLevel` auf das Merkmal „Überschrift“ ab. Die Ebenen wirken nur unter Windows (Sprachausgabe) und Android. Verloren gegangen ist Ebene 1 nur auf „Tankbuch“. Auf Favoriten, Karte und Optionen hatten die entfernten Überschriften gar keine `HeadingLevel`; dort ändert sich für die Sprachausgabe nichts außer dem wegfallenden doppelten Text. Dass „Tankbuch“ jetzt mit Ebene 2 („Fahrzeuge“) beginnt, ist akzeptabel (WCAG 1.3.1 verlangt keine Ebene 1 im Inhalt, der Seitenname steht in der Navigationsleiste). Eine Anpassung der Abschnittsüberschriften auf Ebene 1 empfehle ich nicht. Unabhängig von dieser Änderung haben viele Abschnittsüberschriften gar keine `HeadingLevel` (Karte: „Ansicht“, „Suchen nach“, „Suchradius“, „Spritsorte“, „Sortierung“; Optionen: „Spritsorten“, „Standort und GPS“, „In der Nähe“, „Standardansicht der Suchergebnisse“, „Standardsortierung“, „Datenquelle“; Detail: „Kraftstoffe“, „Öffnungszeiten“, „Favoritengruppen“). Das ist ein bestehender Mangel für die Navigation nach Überschriften und gehört in einen eigenen Schritt (N4).

## Befunde

Keine kritischen und keine hohen Befunde.

### M1 (mittel) – E2E-Prüfung „Titel genau einmal“ kann falsch grün sein (Shell-Titel vor Seiteninhalt)

- **Fundstellen:**
  - `src/Tankradar.Tests.E2E/E2E/FlaUI/NavigationE2ETests.cs` Z. 34–38
  - `src/Tankradar.Tests.E2E/E2ETestBase.cs` Z. 126–128 (`NavigateToTab` ohne AutomationId) und Z. 147–153 (`IsShellTitle`)
  - `src/Tankradar.Tests.E2E/E2E/FlaUI/DiagnosticsCaptureE2ETests.cs` Z. 21–22
- **Problem:** `NavigateToTab(tabTitle)` kehrt zurück, sobald der Text mit der AutomationId `title` den Reiternamen zeigt. Die Shell setzt diesen Titel beim Seitenwechsel, ohne dass der Seiteninhalt schon im UI-Automation-Baum stehen muss. `Assert.Single(...)` zählt unmittelbar danach. Käme eine Überschrift im Inhalt zurück, der Inhalt wäre beim Zählen aber noch nicht aufgebaut, bestünde der Test trotzdem. Beim ersten Reiter „Favoriten“ ist das am wahrscheinlichsten: Der Titel steht dort schon beim Start, `NavigateToTab` kehrt sofort zurück. Bisher bewies die AutomationId der Überschrift, dass der Inhalt gerendert war; diese Prüfabsicht („tatsächlicher Seitenwechsel“) ist jetzt nur noch für die Kopfzeile erfüllt. Ebenso wartet `DiagnosticsCaptureE2ETests` laut Kommentar „auf eine gerenderte Seite“, tatsächlich aber nur auf die Kopfzeile.
- **Einordnung:** Ein Wettlauf mit einem *alten* Titel tritt bei keinem Aufrufer auf. Bei allen Wechseln (Reiterwechsel, Zurück aus Detail und Favoritengruppe) unterscheidet sich der vorherige Titel vom erwarteten. Beim Wechsel auf den bereits aktiven Reiter ist die sofortige Rückkehr korrekt. Alle anderen Aufrufer warten danach ohnehin auf ein Inhaltselement (`OpenSettings`, `OpenSearch`, `OpenFavorites`, `OpenTankbook`, `LeaveDetail` usw.). Der statische Fall ist zusätzlich durch `PageTitleRedundancyTests` abgedeckt. Deshalb mittel und nicht hoch.
- **Korrektur:**
  1. `WaitForPageTitle` (samt dem von ihr genutzten `WaitUntil`) aus `SettingsE2ETestBase` nach `E2ETestBase` verschieben.
  2. In `NavigationE2ETests` je Reiter ein Inhaltselement festlegen und erst danach zählen:
     ```csharp
     private static readonly Dictionary<string, string> ContentIds = new()
     {
         ["Favoriten"] = "Favorites.Refresh",
         ["Karte"] = "Search.Submit",
         ["Tankbuch"] = "Tankbook.AddVehicle",
         ["Optionen"] = "Settings.Loaded",
     };
     // ...
     NavigateToTab(tabTitle, ContentIds[tabTitle]);   // Inhalt ist gerendert
     WaitForPageTitle(tabTitle);                       // Titel steht in der Kopfzeile
     Assert.Single(MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(tabTitle))));
     ```
  3. In `DiagnosticsCaptureE2ETests` `NavigateToTab("Optionen", "Settings.Loaded")` verwenden, damit der Kommentar wieder stimmt.

### N1 (niedrig) – `PageTitleRedundancyTests` vergleicht Rohtexte statt aufgelöster Werte

- **Fundstelle:** `src/Tankradar.Tests.Unit/Unit/PageTitleRedundancyTests.cs` Z. 35–40.
- **Problem:** `Title` und `Label.Text` werden als rohe Attributtexte verglichen. Vier der sieben Seiten setzen den Titel über `{x:Static texts:…PageTitle}`. Ein `<Label Text="Tankbuch" …>` auf `TankbookPage` oder `Text="Tank- und Ladevorgänge"` auf der Vorgangsseite fiele nicht auf. Umgekehrt fiele auf `FavoritesPage` (literaler Titel) ein `Label` mit `{x:Static texts:FavoritesTexts.PageTitle}` nicht auf. Erkannt wird damit nur genau das entfernte Muster (gleicher Markup-Text bzw. AutomationId auf `.Headline`). Zu streng ist der Test nicht: Ein Titel ist für jede Seite nötig, und das Verbot von `*.Headline` ist eng genug.
- **Korrektur:** `{x:Static präfix:Typ.Member}` per Reflexion auflösen. Das Testprojekt referenziert `Tankradar.MAUI`; den Namensraum liefert die `xmlns`-Deklaration `clr-namespace:…`. Dann aufgelöste Werte vergleichen (getrimmt, `StringComparison.OrdinalIgnoreCase`) und `Span.Text` einbeziehen. Dazu einen Negativtest mit einem kleinen XAML-Text, der eine literale und eine `x:Static`-Wiederholung enthält und vom Prüfcode erkannt werden muss (Prüfcode dafür in eine Methode `FindRepeatedTitles(XDocument)` auslagern).

### N2 (niedrig) – Optionen: unsichtbarer Prüftext ist jetzt erstes Element (Abstand oben, erste Ansage)

- **Fundstelle:** `src/Tankradar.MAUI/Views/SettingsPage.xaml` Z. 42–47 (`Settings.Loaded`).
- **Problem:** Das Label „Einstellungen geladen“ (`Opacity="0"`, `HeightRequest="1"`, sichtbar sobald `IsLoaded`) stand bisher zwischen Überschrift und erster Karte und steht jetzt ganz oben. Im `VerticalStackLayout` mit `Spacing` 16 ergibt das 16 + 1 + 16 = 33 px bis zur ersten Karte. Auf allen anderen Seiten sind es 16 px; der Unterschied fällt jetzt am oberen Rand auf. Unter Windows steht der Text im UI-Automation-Baum (die E2E-Tests brauchen ihn), die Sprachausgabe liest ihn dort jetzt als erstes Element der Seite. Ob VoiceOver ihn auf iOS überspringt (Alpha 0), ist am Gerät zu prüfen.
- **Korrektur:** Das Label ans Ende des `VerticalStackLayout` verschieben (nach der Karte „Datenquelle“). Die E2E-Tests prüfen nur, dass es existiert. Damit beginnt die Seite wie alle anderen 16 px unter der Navigationsleiste, und der Text wird zuletzt vorgelesen.

### N3 (niedrig) – Tankbuch: übrig gebliebener Container mit nur einem Element

- **Fundstelle:** `src/Tankradar.MAUI/Views/TankbookPage.xaml` Z. 25–29.
- **Problem:** Das innere `VerticalStackLayout` (`Spacing` xs) hielt Überschrift und Einleitung zusammen. Jetzt enthält es nur noch die Einleitung. Optisch ändert sich nichts, die Verschachtelung ist aber überflüssig. Auf der Vorgangsseite ist der entsprechende Container mit Fahrzeugname, Details und Einleitung weiter sinnvoll gefüllt.
- **Korrektur:** Das Einleitungs-`Label` direkt in den äußeren Stack setzen und den Wrapper entfernen.

### N4 (niedrig) – Überschriftenebenen uneinheitlich

- **Fundstellen:**
  - `src/Tankradar.MAUI/Views/StationDetailPage.xaml` Z. 133–136 (`Detail.Name`)
  - `src/Tankradar.MAUI/Views/FavoriteGroupPage.xaml` Z. 60–63 (`FavoriteGroup.Name`)
  - im Gegensatz zu `src/Tankradar.MAUI/Views/TankbookEntriesPage.xaml` Z. 34 (Fahrzeugname, jetzt `Level1`)
  - Abschnittsüberschriften ohne `HeadingLevel`: siehe Abschnitt „iOS und Sprachausgabe“
- **Problem:** Die Objektüberschriften der Unterseiten sind im Inhalt jetzt die obersten Überschriften. Auf der Vorgangsseite ist das als Ebene 1 ausgezeichnet, auf Detail und Favoritengruppe gar nicht. Die Navigation nach Überschriften (VoiceOver-Rotor, Windows-Sprachausgabe) überspringt sie dort. Das bestand schon vorher und ist keine Folge dieser Änderung, fällt aber jetzt stärker ins Gewicht.
- **Korrektur:** `SemanticProperties.HeadingLevel="Level1"` an `Detail.Name` und `FavoriteGroup.Name`. Die Abschnittsüberschriften auf Karte, Optionen und Detail in einem eigenen Schritt einheitlich als `Level2` auszeichnen, mit einer E2E-Prüfung der UIA-Eigenschaft `HeadingLevel`, wie sie es für die Routensuche schon gibt.

### N5 (niedrig) – Kommentare und Doku nachziehen

- `src/Tankradar.Tests.E2E/E2E/FlaUI/TankbookE2EDriver.cs` Z. 25: Die Parameterbeschreibung „(Titel, AutomationId der Zielseite)“ erwähnt nicht, dass `null` jetzt den Seitentitel der Shell prüft. Ergänzen: „…; <see langword="null"/> prüft den Seitentitel der Shell“.
- `README.md`: Die Korrektur 0.1.26 steht dort (Z. 13), für 0.1.27 fehlt eine entsprechende Zeile. Eine kurze Zeile ergänzen, z. B. „Korrektur (Version 0.1.27): Der Seitenname steht nur noch in der Navigationsleiste, nicht mehr zusätzlich als Überschrift im Inhalt (Favoriten, Karte, Tankbuch, Optionen, Tank- und Ladevorgänge).“
- `changes.log` Z. 6: „Die Texte (`PageTitle`-Konstanten) werden weiter für die Titel verwendet“ stimmt nur für Tankbuch, Vorgänge, Stationsdetails und Favoritengruppe. Favoriten, Karte und Optionen haben literale Titel in XAML und `AppShell.xaml`; `FavoritesTexts.PageTitle` wird nur im ViewModel gesetzt, für Karte und Optionen gibt es keine Konstante. Satz präzisieren oder (optional) die drei Titel auf Konstanten umstellen.
- `docs/help/Navigation/beschreibung.md` (Anwendersicht): Optional einen Satz ergänzen, dass der Name des Bereichs oben in der Titelleiste steht.

### N6 (niedrig, optional – Entscheidung des Anwenders) – Tankbuch: Einleitung „Deine Fahrzeuge.“ direkt vor dem Abschnitt „Fahrzeuge“

- **Fundstelle:** `src/Tankradar.MAUI/Resources/Texts/TankbookTexts.cs` Z. 26 (`Intro`), `TankbookPage.xaml` Z. 26 und Z. 165.
- **Problem:** Ohne die Überschrift beginnt die Seite mit „Deine Fahrzeuge. Alle Angaben bleiben auf diesem Gerät.“. Bei geschlossenem Formular folgt direkt die Abschnittsüberschrift „Fahrzeuge“. Das ist keine Wiederholung des Titels, aber eine leichte inhaltliche Dopplung im Sinne des Wunsches „überflüssige Überschriften bereinigen“.
- **Korrektur (optional):** Die Einleitung auf „Alle Angaben bleiben auf diesem Gerät.“ kürzen, analog zur Vorgangsseite, und betroffene Texttests anpassen.

### Nebenbefund außerhalb des Umfangs

- `FavoritesTexts.GroupsHeading` („Favoritengruppen“, `FavoritesTexts.cs` Z. 26) ist seit Schritt 10 (`b11a01d`) nie verwendet worden. Das hat mit dieser Änderung nichts zu tun; die Konstante kann bei Gelegenheit entfernt werden.

## Ausgeführte Prüfungen

Alle Läufe in der Shell mit `IncludeAndroidTarget=false` und `IncludeMacCatalystTarget=false`, ohne `FUEL_PRICE_API_KEY` (`env -u`), ohne produktive Endpunkte (E2E nur gegen Mock-Server).

| Prüfung | Ergebnis |
|---|---|
| `dotnet build Tankradar.sln -c Release -p:TreatWarningsAsErrors=true` (Windows und `net10.0-ios`/`iossimulator-x64`) | erfolgreich, 0 Warnungen, 0 Fehler |
| dasselbe mit `--no-incremental` | erfolgreich, 0 Warnungen, 0 Fehler |
| `dotnet format Tankradar.sln --verify-no-changes --no-restore --severity error` | keine Abweichungen |
| `.githooks/format-code-style-check.py --strict` (`dotnet format --verify-no-changes` mit Standard-Schweregrad, wie im Pre-Push) | OK |
| Unit-Tests vollständig (`Tankradar.Tests.Unit`, Release) | 1938 von 1938 bestanden, davon 7 Fälle `PageTitleRedundancyTests` (alle sieben `*Page.xaml`) |
| E2E off-screen (`TANKRADAR_E2E_WINDOW` nicht gesetzt), gezielt: `NavigationE2ETests`, `DiagnosticsCaptureE2ETests`, `FavoritesE2ETests_*` (Groups, DisplayName, Flow), `HomeE2ETests_Overview`, `HomeE2ETests_OpenDetail`, `DetailE2ETests_Open`, `MapE2ETests_*` (Markers, Navigation, View), `SearchE2ETests_Basic`, `SettingsE2ETests_Defaults`, `SettingsE2ETests_FuelTypes`, `TankbookE2ETests_Flow`, `TankbookEntriesE2ETests_Flow`, `TankbookEntriesE2ETests_RestoreReload`, `OfflineE2ETests_Tankbook` | 42 von 42 bestanden (3,5 min). Damit sind alle geänderten E2E-Dateien abgedeckt. Die Windows-Kopfzeile (`title`) zeigt den Titel auf allen vier Reitern und nach der Rückkehr aus der Detailansicht zur Karte bzw. zu den Favoriten. Einmal „Fatal error. 0xC0000005 … GC.RunFinalizers“ beim Beenden einer App-Instanz; das ist bekannt (ADR 0015), die Tests sind nicht betroffen |
| `forbidden-patterns-check.py --all --strict` | OK (1073 Dateien) |
| `csproj-xmldoc-check.py --all` | OK (841 Dateien) |
| `translation-check.py --all` | OK (keine `.resx`-Dateien) |
| `enum-coverage-check.py --all --strict` | OK |
| `no-notimplemented-check.py --all --strict` | OK (837 Dateien); die neuen und geänderten Testhilfen enthalten keine Methoden, die nur `throw` enthalten |
| Code-Durchsicht | alle sieben Seiten-XAML vollständig, `AppShell.xaml(.cs)`, `App.xaml.cs`, `StationTile`/`StationMapView`, Texte (`Resources/Texts/*`), `Title` in den ViewModels, Suche nach Navigationsleisten-Einstellungen, modalen Seiten und Dialogen, alle E2E-Aufrufer von `NavigateToTab`/`WaitForPageTitle`, Doku (`changes.log`, `docs/help/**`, README), Restverweise auf `*Page.Headline` (nur noch in historischen Abnahmeberichten) |

Nicht ausgeführt (nicht beauftragt oder nicht möglich):
- vollständiger `local-ci`-Lauf, Integrationstests und die übrigen E2E-Tests
- Commit-Nachrichten- und Testausführungsprüfung des Pre-Push-Hooks
- Gerätetest auf dem iPhone (VoiceOver, Navigationsleiste)
- Start des Windows-Zwischenstands `review-versions/0.1.27_*` (bewusst nicht gestartet)

Abgelehnte Aktionen: keine. Am Repository habe ich außer diesem Bericht nichts geändert; der Bericht ist nicht committet. Die Worktrees `../ta-fix` und `../ta-17a` habe ich nicht angefasst.

Prozesse: Build-, Format- und Testprozesse sowie alle von den E2E-Tests gestarteten App-Instanzen sind von selbst beendet; kein `Tankradar.MAUI.exe` läuft mehr. Übrig sind wiederverwendbare MSBuild-Knoten (`dotnet.exe … MSBuild.dll /nodeReuse:true`, gestartet 18:05:09–18:05:24, also im Zeitfenster meines ersten Builds). Ich habe sie nicht beendet: Ihre Elternprozesse sind beendet, und ich kann sie deshalb nicht sicher mir zuordnen. Um 18:04 und 18:16 liefen parallel fremde `dotnet`/`testhost`-Prozesse, die diese Knoten mitbenutzen können. Die Knoten beenden sich nach der MSBuild-Leerlaufzeit von selbst.

## Nachtrag: Korrekturen nach der Abnahme (Commit 49a005d)

Behoben: M1 (Navigationstest wartet erst auf Seiteninhalt, dann Titel, dann Zählung; auch Diagnosetest), N1 (`x:Static` aufgelöst, Span geprüft, Negativtest), N2, N3, N4 (Level1 für Stations- und Gruppenname), N5, ungenutzte Konstante `FavoritesTexts.GroupsHeading` entfernt. N6 (Einleitung „Deine Fahrzeuge.“) wartet auf die Entscheidung des Anwenders.
Geprüft nach dem Merge: Release-Build mit Warnungen als Fehler, Unit 1940/1940, E2E Navigation und Offline-Tankbuch grün.
