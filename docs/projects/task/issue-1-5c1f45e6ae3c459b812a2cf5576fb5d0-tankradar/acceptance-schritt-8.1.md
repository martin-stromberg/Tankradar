# Abnahmeprüfung – Entwicklungsschritt 8

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Designabweichungen der Detailansicht nicht dokumentiert.** Laut den Vorgehensentscheidungen („Designentwurf … verbindlich. Jede Abweichung … wird in der Projektdokumentation festgehalten“) und dem Prüfauftrag müssen Abweichungen dokumentiert sein. `StationDetailPage.xaml` weicht deutlich vom Entwurf `tankstellen_details_favoriten` ab. Für die Suche gibt es dafür ADR 0002, für die Detailansicht weder ein ADR noch einen anderen Eintrag. `docs/help/Tankstellendetails/beschreibung.md` behauptet sogar „nach dem Designentwurf ‚Stationsdetails‘“. Nicht dokumentiert sind insbesondere:
  - Die Info-Box (Entfernung und Öffnungsstatus mit Symbolen, Chip „Automat 24/7“) ist als Textzeilen umgesetzt; der Chip „Live-Preise“ fehlt.
  - Die Preisdarstellung weicht ab: „1,859 €“ statt der hochgestellten 9 mit „€/L“, ohne Untertitel (Sortenbezeichnung, Trend).
  - Hinzugefügt wurden eine eigene Schaltfläche „Zurück“ (zusätzlich zum Zurück-Pfeil der Shell-Kopfleiste), die Schaltfläche „Preise aktualisieren“, der Quellenhinweis und die Öffnungszeiten-Karte mit „Stand“.
  - Weggelassen wurden Preisverlauf/Spartipp, „Ausstattung & Services“, Favoritengruppen, Stern sowie „Im Tankbuch erfassen“ und „Navigation“. Teilweise gehören sie zu späteren Schritten oder fehlen mangels Datenquelle; auch das ist nirgends festgehalten.

  Nur der Wegfall von Strom/EV ist über ADR 0001 abgedeckt.
- [ ] **Zusatz 3: Der Schlüssel aus `FUEL_PRICE_API_KEY` gelangt bei diagnostischer MSBuild-Ausführlichkeit ins Build-Log.** Gefordert ist „Schlüssel nie im Repo/Logs/Build-Ausgaben“; der csproj-Kommentar sagt „wird … nie ausgegeben“.
  - **Nachweis** (nur mit einem Platzhalterwert `ZZDUMMYSECRETZZ`, echter Schlüssel per `env -u` entfernt): `dotnet msbuild Tankradar.MAUI.csproj -t:DoesNotExistTarget` (Fehlerfall).
    - Bei `-v:n` erscheint der Wert 0-mal.
    - Bei `-v:diag` erscheint er 4-mal: „Eigenschaft … aus der Umgebung erweitert“, Neuzuweisung von `TankerkoenigApiKey`, Eigenschaftsliste `TankerkoenigApiKey = …` und das Metadatum `_Parameter2` des `AssemblyAttribute`-Items.
  - **Neu durch diesen Schritt:** MSBuild protokolliert nur Umgebungsvariablen, die das Projekt verwendet. Erst durch `$(FUEL_PRICE_API_KEY)` im csproj landet die beim Anwender dauerhaft gesetzte Variable in jedem lokalen `-v:diag`-/`-v:detailed`-Log und jeder Binlog (`-bl`). Zudem wird der echte Schlüssel jetzt in jeden lokalen Build eingebettet, auch in die Builds von `local-ci` und der Tests.
  - Bei normaler Ausführlichkeit, auch im Fehlerfall, wird nichts ausgegeben. Die CI verwendet weder `-v:diag` noch `-bl`. Eine Schutzmaßnahme (z. B. Übernahme nur bei Release-/Paket-Builds oder ein Hinweis in `api-schluessel.md`) oder eine Richtigstellung der Doku fehlt.

## Hinweise

- **Praktische Verifikation**
  - `scripts/local-ci.ps1` lief einmal vollständig, Exit-Code 0, alle Schritte OK:
    - Pipeline-Skripte, Restore, Format, Sicherheitsprüfung, Build mit Warnungen als Fehler
    - Unit-Tests 532/532, Integrationstests 82/82, Abdeckung 95,2 %
    - FlaUI-E2E 44/44, off-screen, 2 min 23 s
    - iOS-Compile-Prüfung
  - `scripts/test-ios-deployment.ps1`: Exit-Code 0.
  - Im `local-ci`-Log stehen keine schlüsselartigen Werte: Alle UUID-förmigen Treffer sind Repository-Pfade bzw. TestResults-Verzeichnisse. Geprüft wurde maskiert, ohne den echten Schlüssel auszulesen.
- **Testmodus und echter Schlüssel**
  - `ApiKeyProvider` liefert im Testmodus (`TANKATLAS_TEST_DATA_PATH`) ausschließlich `TANKRADAR_PRICE_API_KEY` und nie den Build-Schlüssel.
  - Preis- und Geokodierungsendpunkte sind im Testmodus nur über `TANKRADAR_PRICE_API_URL`/`TANKRADAR_GEOCODING_URL` möglich, sonst wird die Anfrage verweigert. Die E2E-Basis setzt Mock-URLs.
  - Ein gesetztes `FUEL_PRICE_API_KEY` führt daher nicht zu Aufrufen des echten Dienstes.
- **Reihenfolge der Schlüsselquellen:** per `-getProperty` mit Platzhaltern bestätigt: `TANKRADAR_FUEL_PRICE_API_KEY` vor `FUEL_PRICE_API_KEY`. Ohne beide ist der Wert leer; die lokale props-Datei wird über `_TankerkoenigLocalKey` als Letztes herangezogen. `tankerkoenig.local.props` und `*.local.props` stehen in `.gitignore`, `obj/` und `bin/` ebenfalls. Es gibt keinen automatisierten Test für die Reihenfolge.
- **Detailansicht fachlich erfüllt**
  - Angezeigt werden Name, Adresse, Entfernung (aus der Suche), Preise der aktivierten Sorten in der Reihenfolge der Einstellungen mit „vor X Min.“ und Amber ab 60 Minuten.
  - Die Hinweise „Preis unbestätigt“ und „Automatentankstelle“ erscheinen, ebenso Öffnungszeiten mit Stand.
  - Zahlungsmöglichkeiten liefert Tankerkönig nicht; das ist in der Hilfe dokumentiert. Fehlende Angaben werden ausgeblendet. Es gibt keine Strom-/Ladeinformationen.
  - Abgedeckt durch Unit-Tests (`StationDetailBuilderTests_*`) und zwei FlaUI-Tests (`DetailE2ETests_Open`).
- **Offline und Wiederverbindung**
  - Ohne Verbindung zeigt die Ansicht die zuletzt bekannten Daten mit Alter, den Quellenhinweis und das Offline-Banner (auch ohne lokale Daten bleibt die Listenanzeige stehen).
  - `ConnectionRestored` fragt neu ab, wenn der letzte Stand `OfflineFallback` war oder ein Preis veraltet ist; danach entfällt das Banner.
  - Laufende Abrufe werden bei Neuabruf oder Verlassen der Seite abgebrochen, späte Ergebnisse verworfen.
  - Durch Unit-Tests abgedeckt (`StationDetailViewModelTests_Offline`/`_Concurrency`).
  - Das Banner steht oben im scrollbaren Inhalt, nicht fest in der Kopfleiste; beim Scrollen verschwindet es.
  - „Preise aktualisieren“ bleibt auch offline bedienbar und führt dann zum Offline-Stand. Das ist zulässig, die Anforderung verlangt keine Sperre.
- **Zusatz 1:** Die Altersgrenze von 24 h (`DetailFreshness`) gilt in der Detailansicht, in `StationResultBuilder` und in `FuelPriceService.WithKnownDetailsAsync`. Detailangaben ohne Zeitstempel werden ausgeblendet. Getestet.
- **Zusatz 2:** ’ ‘ ʼ werden zu `'`, – — ‐ ‑ − zu `-`. Tests mit „Up’n Kamp“ gibt es auf Validierungs- und Nominatim-Ebene. Typografische Anführungszeichen („ “ …) werden weiterhin abgelehnt; die Smart-Punctuation von iOS kann solche erzeugen.
- **Zusatz 4:** `docs/help/ci-cd/workflows.md` hält fest, dass die CI-E2E über `TANKRADAR_E2E_WINDOW=foreground` im Vordergrund laufen, mit Begründung (Timeout `0x800705B4`).
- **Zusatz 5: Drosselung**
  - Der Standardabstand beträgt 1100 ms; die Validierung verlangt weiterhin mindestens 1 s.
  - `RequestThrottle` nutzt die monotone Uhr (`GetTimestamp`/`GetElapsedTime`, Test mit Wanduhrsprung) und serialisiert parallele Anfragen über `SemaphoreSlim`.
  - Bei Abbruch während der Wartezeit wird kein Zeitstempel gesetzt. Bei Abbruch nach dem Senden bleibt der Zeitstempel bestehen (konservativ).
  - Der E2E-Test prüft am Mock ≥ 1000 ms und war grün.
- **Zwischenstand `review-versions/0.1.11_2026-10-06/`:** vorhanden (`bin/`, `CHANGELOG.md` zu Schritt 8) und per `.gitignore` ausgeschlossen. Ein kurzer Start mit `TANKATLAS_TEST_DATA_PATH` (temporäres Verzeichnis) und `TANKATLAS_TEST_WINDOW=offscreen` öffnete das Fenster „Tankatlas“. Danach wurde der Prozess gezielt beendet und das temporäre Verzeichnis entfernt.
- Im Arbeitsverzeichnis liegt weiterhin das unversionierte `docs/features/` (Restbestand aus Schritt 2, nicht Teil dieses Schritts).
