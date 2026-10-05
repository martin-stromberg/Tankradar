# Abnahmeprüfung – Entwicklungsschritt 6

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

**Abweichung aus `acceptance-schritt-6.2.md` (Radius 1 km): behoben.**

- `SearchRadius.Steps` umfasst jetzt 1, 2, 5, 10, 15 und 25 km. Das entspricht genau der Vorgehensentscheidung „Suchradius“: 1 bis 25 km, Standard 5 km, Chips gemäß Designentwurf. Die Oberfläche (`MapPage.xaml`, `FlexLayout` mit Umbruch über `RadiusOptions`) bietet damit sechs Chips an, 5 km ist vorgewählt.
- Der E2E-Test `SearchE2ETests_Radius.Radius1_ShowsOnlyNearestStation` ist wieder vorhanden. Er wählt den Chip „1“ und prüft, dass nur „Alpha Tankstelle“ erscheint und der Mock genau Radius 1 empfängt. Im Prüflauf ist er bestanden.
- Zur Begründung in ADR 0002:
  - Der Kontext nennt jetzt „1 bis 25 km, Standard 5 km, Stufen 1, 2, 5, 10, 15, 25“.
  - Der neue Punkt „Stufen-Chips statt freier Eingabe“ begründet die Chip-Auswahl: Designentwurf, Abdeckung des Bereichs, Bedienbarkeit, keine Fehleingaben.
- Die Hilfe ist jetzt widerspruchsfrei. `docs/help/Suche/index.md` nennt „1 bis 25 km“. `beschreibung.md` (Ablauf und Einschränkungen) und `ablauf-anwender.md` nennen die Stufen 1/2/5/10/15/25. `ablauf-technisch.md` und `README.md` sind angeglichen. Veraltete Angaben zu den Stufen „2/5/10/15/25“ gibt es in Code und Doku nicht mehr.

**Umbau `RadiusText` auf `RadiusKm`: ohne Regression.**

- `MapViewModel.RadiusKm` ist jetzt ein `int`. `SearchAsync` prüft vor Standortabfrage und API-Aufruf mit `SearchRadius.IsValid` (1–25). Ein ungültiger Wert führt zu `SearchTexts.RadiusInvalid`, ohne Standort- oder Preisabruf. Der Preisdienst prüft den Radius zusätzlich (`FuelPriceService`, 1 bis `MaxRadiusKm`).
- `SyncRadiusSelection` markiert nur den passenden Chip. Ein Wert außerhalb der Stufen (z. B. 7) markiert keinen Chip.
- Die Tests sind sinnvoll umgestellt:
  - `InvalidRadius_CallsNeitherLocationNorPrices` prüft 0, 26 und −3.
  - `ValidRadius_IsSentUnchangedAndNeverAbove25` prüft 1, 5 und 25.
  - `RadiusOutsideSteps_DeselectsChipsAndInvalidIsRejected` sowie die Integrationstests (`SearchMockServerTests_Flow`, `SearchPrivacyTests_Persistence`) arbeiten mit `RadiusKm`.
  - Die früheren Textfälle („5.5“, „abc“, „“) gibt es auf ViewModel-Ebene nicht mehr, weil es keine Texteingabe mehr gibt. `SearchRadiusTests_Validation` deckt sie weiter für `TryParse` ab.
- Es gibt keine Verweise mehr auf `RadiusText` in `src`, Hilfe oder README. Die gebundene XAML nutzt nur `RadiusOptions`.
- Robustere Paging-Benachrichtigung: `PublishVisible` meldet `TotalStationCount`, `HasMore` und `ShowMoreText` jetzt bei jeder Neuberechnung, nicht mehr nur bei einer Änderung von `Stations`. Neue Unit-Tests:
  - `FilterChange_ResetsPaging` schließt die in 6.2 genannte Testlücke.
  - `SortChange_NotifiesShowMoreProperties` prüft die Benachrichtigung bei einem Sortierwechsel.

**Gesamte Anforderung erneut geprüft (unverändert erfüllt):**

- Umkreissuche am aktuellen Standort mit einstellbarem Radius.
- Filter nach Spritsorte („Alle“ und die gewählten Sorten).
- Die Ergebnisliste zeigt je Tankstelle:
  - Name, Entfernung und Adresse.
  - Preise der in den Einstellungen gewählten Sorten, in deren Reihenfolge.
  - „Preis unbestätigt“ und „Automatentankstelle“, soweit Details bekannt sind.
- Sortierung nach Preis, Entfernung oder Name, voreingestellt aus den Einstellungen.
- Altersangabe „vor X Min.“, ab 60 Minuten in Amber.
- Ohne Verbindung die zuletzt bekannten Preise mit Offline-Hinweis.
- Standort:
  - Wird nur beim Auslösen der Suche und gemäß GPS-Einstellung abgefragt.
  - Bei „Nie“ keine Abfrage, sondern ein Hinweis.
  - Keine Speicherung von Rohkoordinaten (Datenschutz-Tests für Persistenz und Logging).
- Der iOS-Zwecktext `NSLocationWhenInUseUsageDescription` lautet „Ermittlung von Tankstellen in der Nähe“.
- Radius und Filter werden vor dem API-Aufruf validiert.
- Design nach Entwurf, Abweichungen in ADR 0002 begründet.
- Testmodus-Variable `TANKATLAS_TEST_DATA_PATH`.
- Die Tests decken Filterung, Sortierung, Validierung, Standortberechtigung, Altersanzeige und per FlaUI den Suchablauf mit Testdaten (Mock-Server) ab.

**Weiterhin offene, nicht abnahmerelevante Beobachtungen (aus 6.1/6.2):**

- `SearchRadius.TryParse` wird im Produktivcode nicht mehr verwendet, nur noch in `SearchRadiusTests_Validation`. Das ist toter Code. Er kann entfernt werden, oder er bleibt für die Adresssuche (Schritt 7), falls dort ein Radius eingegeben werden soll.
- Der Auswahlzustand der Chips wird für Bedienhilfen nur als `SemanticProperties.Hint` („Ausgewählt“) bereitgestellt. Es gibt kein echtes `Selected`-Trait und kein UIA-Pattern.
- Die drei Chip-`DataTemplate`s in `MapPage.xaml` sind weitgehend identisch.
- Jedes „Weitere anzeigen“ baut alle bereits sichtbaren Karten neu auf.
- Die Detailangaben (Öffnungszeiten) haben keine Altersgrenze. Empfehlung: spätestens in Schritt 8 eine festlegen.
- Altersangaben werden nicht laufend aktualisiert, und der Offline-Hinweis scrollt mit dem Inhalt mit.
- Kleinere Designunterschiede ohne Erwähnung im ADR: Preisformat, Position der Entfernung, Filterkarten statt kompakter Chip-Leiste.

**Praktische Verifikation:**

- **`scripts/local-ci.ps1` (einmal, inkl. E2E):** Exit-Code 0, „Lokaler Prüflauf erfolgreich“. Im Einzelnen:
  - Pipeline-, Workflow- und iOS-Deployment-Prüfungen (beide Umgebungen), Restore, Format und Sicherheitsprüfung: OK.
  - Build mit Warnungen als Fehler: 0 Warnungen, 0 Fehler.
  - Tests: 353 Unit-Tests und 58 Integrationstests bestanden, Zeilenabdeckung 94,4 % (Schwelle 70 %). 33 FlaUI-E2E-Tests bestanden (einer mehr als in 6.2, nämlich `Radius1_ShowsOnlyNearestStation`), ohne Wiederholung.
  - iOS-Compile-Prüfung: OK, mit der bekannten Warnung `MAUI1001`.
- **`scripts/test-ios-deployment.ps1`:** „iOS-Deployment-Pruefung erfolgreich.“ (Exit-Code 0).
- **Zwischenstand `review-versions/0.1.9_2026-10-05/`:**
  - Vorhanden und per `.gitignore` ausgeschlossen.
  - `CHANGELOG.md` ist gefüllt: 1-km-Stufe, robuste Schaltfläche „Weitere anzeigen“, vereinheitlichte Hilfe, README und ADR.
  - Die DLL enthält `RadiusKm`.
  - Ich habe ihn mit `TANKATLAS_TEST_DATA_PATH` auf ein temporäres Verzeichnis unter `%TEMP%` gestartet. Das Fenster „Tankatlas“ erschien, und die Datenbank `tankatlas.db` lag im Testverzeichnis.
  - Danach habe ich ihn sofort per Name `Tankradar.MAUI` beendet und das Testverzeichnis gelöscht. Es läuft kein Prozess mehr. Die UI habe ich nicht bedient.
- Ich habe keinen echten Tankerkönig-Endpunkt aufgerufen, nichts committet oder gepusht und keine Konfiguration geändert. Der Arbeitsbaum ist bis auf diesen Bericht unverändert.
