# Anforderung: Schritt 17 - Tankstellen entlang einer Route

## Fachliche Zusammenfassung
Die Tankstellensuche im Bereich "Karte" erhält neben Standort- und Adresssuche (`SearchMode`) einen dritten Modus "Route". Start und Ziel werden über die bestehende Nominatim-Geokodierung (`NominatimGeocodingService`) aufgelöst, der Start optional aus dem aktuellen Standort (nur bei erlaubter GPS-Nutzung, `GpsUsage`). Die Route (OpenRouteService Directions, Profil Auto, nur HTTPS) wird als Polylinie bezogen; entlang dieser werden kombinierte Tankerkönig-Umkreisabfragen (`TankerkoenigClient`) gestellt, und Stationen innerhalb eines einstellbaren Abstands zur Route werden in Liste und Karte angezeigt. Die Entfernungssortierung bezieht sich auf die Position entlang der Route vom Start.

## Fachpunkte
1. **Eingabe Start/Ziel** (Adresse, Ort oder PLZ), Validierung vor jedem externen Aufruf (`AddressInput`-Muster).
2. **Auswahl/Identifikation:** Start wahlweise "Aktueller Standort" (nur wenn GPS-Einstellung es erlaubt, sonst nicht anwählbar bzw. verständlicher Hinweis) oder Adresseingabe; Ziel immer Adresse. Bei mehrdeutigen Treffern gilt das Verhalten der Adresssuche (Annahme: erster Treffer, wie dort).
3. **Geokodierung** über dieselbe Dienstklasse; die Regel max. 1 Anfrage/Sekunde (Nominatim) gilt auch für zwei Anfragen (Start, Ziel) hintereinander.
4. **Routing** über neuen Client (Vorschlag `OpenRouteServiceClient` / `IRoutingService`), Zeitlimit, Wiederholung mit zunehmender Wartezeit (Muster `TankerkoenigClient`/`Throttling`), nur HTTPS.
5. **Stationsauswahl entlang der Route:** Abfragepunkte entlang der Polylinie (Vorschlag `RouteSampler`), Kombination der Umkreisabfragen (Radius max. 25 km je Abfrage), Zusammenführen/Deduplizieren, Filter auf einstellbaren Abstand zur Route (Punkt-Strecke-Abstand), Berechnung der Position entlang der Route (Streckenkilometer). Anzahl und Takt der Abfragen so bemessen, dass die Tankerkönig-Nutzungsbedingungen eingehalten werden; Obergrenze für sehr lange Routen; Entscheidung in **ADR 0014** mit Begründung (Abweichung von der früheren Entscheidung "keine zusammengeführten Abfragen für größere Radien" ist dort zu begründen).
6. **Darstellung:** Liste und Karte mit gewohnten Spritsortenfiltern, Preisen mit Altersangabe, Zusatzhinweisen, Preisniveau-Farben, Sortierungen; Sortierung "Entfernung" = Position entlang der Route; Route als Linie auf der Karte; Antippen öffnet Detailansicht.
7. **Einstellbarer Abstand zur Route** (Auswahl analog `SearchRadius`/Radius-Chips; konkrete Stufen offen).
8. **Quellenangabe** OpenRouteService und OpenStreetMap sichtbar.
9. **Fehlermeldungen:** Ort nicht gefunden, Route nicht berechenbar, keine Verbindung, Routing nicht verfügbar (fehlender Schlüssel), Obergrenze/zu lange Route; übrige App bleibt nutzbar.
10. **Datenschutz:** Routen, Adressen und Standort werden nicht dauerhaft gespeichert (kein DB-Eintrag, keine Sicherung/Export, keine Protokollierung im Klartext). Preise der Stationen dürfen wie bisher gecacht werden.
11. **API-Schlüssel `ROUTING_API_KEY`:** wie `FUEL_PRICE_API_KEY` (Build-Einbindung über MSBuild/Secret bzw. nicht versionierte lokale Konfiguration, zur Laufzeit Keychain/Credential Locker via `SecureStorageApiKeyStore`/`ApiKeys`, nie im Code); Workflows dürfen das Secret referenzieren (kein Secret wird gesetzt); Dokumentation in ADR, README, `docs/help`. Der Schlüsselwert wird nie gelesen oder ausgegeben.
12. **Tests:** Mock-Server `MockOpenRouteServiceServer` in `src/TestSupport` (auf `LoopbackHttpListener`); keine Produktivendpunkte. Unit-Tests: Stationsauswahl (Abstand, Reihenfolge, Abfragepunkte, Obergrenze), Polylinie-Dekodierung, Geometrie, Validierung, Fehlerverhalten, Wiederholung. FlaUI-E2E für Suchablauf ohne Fokusdiebstahl (nur UIA-Muster, `SetFieldValue`; vgl. `E2EInputPolicyTests_NoFocus`), keine breiten `FindAll` in Wartephasen mit großen Listen (`TransientWait`).
13. **iOS:** `MtouchInterpreter=all` und `TrimmerRootAssembly` unverändert; keine neue Abhängigkeit (Polyline-Dekodierung und Geometrie selbst implementiert).
14. **Zwischenstand:** nach Abschluss Windows-Zwischenstand unter `review-versions/<Version>_<JJJJ-MM-TT>/` (nicht committen).

## Zusatz aus der Abnahme von Schritt 16 (Härtungen, optional)
- `SearchE2ETests_LargeResult` gegen Falsch-Grün schärfen: Namensreihenfolge nach Sortierwechsel prüfen; einmal 50 dargestellte Einträge zählen, sobald die App ruht; 10-s-Grenze auf Messzeit ohne UIA-Wartezeit beziehen; Verhalten in den nächsten CI-Läufen beobachten.
- Beleg-E2E-Test gegen `ElementNotAvailableException` zwischen Suche und Lesen härten.
- Dokumentation: `docs/help/ci-cd/workflows.md`; ADR 0013 (nur die Vorgangsseite des betroffenen Fahrzeugs schließt); `docs/help/Sicherung/ablauf-technisch.md` (Abonnent von `IRestoreNotifier`; laut Abnahme 16.3 bereits aktualisiert, prüfen).
- Namen der Schaltfläche "Weitere anzeigen (N weitere)" für die Sprachausgabe entdoppeln (AutomationId/Name; E2E-Test liest N aus dem Namen und ist anzupassen).

## Betroffene Klassen und Komponenten (Vorschläge, Namen nicht verbindlich)
- Modelle: `SearchMode` (neuer Wert Route), `RouteResult`/`RoutePoint` (flüchtig), `RouteStationItem` bzw. Erweiterung `StationListItem` um Streckenposition, Abstand zur Route; Fehlerart-Enum (z. B. `RoutingFailure`) mit Testabdeckung aller Werte.
- Services: `IRoutingService`, `OpenRouteServiceClient`, `RoutingOptions`, `PolylineDecoder`, `RouteGeometry`, `RouteStationSearchService` (Abfragepunkte, Takt, Obergrenze), Erweiterung `ApiKeys`/Schlüsselablage für Routing-Schlüssel, Registrierung in `MauiProgram`.
- ViewModels/Views: `MapViewModel` bzw. Such-ViewModel und Karten-/Suchseite (Routenmodus, Eingabefelder, Abstandsauswahl, Quellenangabe), Routenlinie in Kartenzeichnung, AutomationIds.
- Build: `Tankradar.MAUI.csproj`/`MSBuild/`, `.github/workflows`, `scripts/` (ROUTING_API_KEY analog FUEL_PRICE_API_KEY).
- Tests: `MockOpenRouteServiceServer`, Unit-, Integration-, E2E-Tests; Übersetzungs-/Enum-/Format-/XML-Doku-Prüfungen.
- Doku: ADR 0014, `docs/help/Suche`, README, `changes.log`.

## Implementierungsansatz
Wiederverwendung von Geokodierung, Preisdienst (inkl. Drosselung `Throttling`, Cache), Listen-/Kartenaufbau (`StationResultBuilder`, `MapMarkerBuilder`, `PriceLevelClassifier`) und Kartenviewport. Neu sind Routing-Client, Polylinien-Dekodierung, Streckengeometrie und die Orchestrierung der kombinierten Umkreisabfragen. Der Datenschutz-Anteil (nicht speichern) erfordert, dass Routenzustand nur im Speicher liegt (kein Eintrag in `AppSettings`/Datenbank/Sicherung).

## Konfiguration
Abstand zur Route als Sucheinstellung (Annahme: wie Radius in der Suche, ggf. nicht persistent bzw. persistent als reiner Zahlenwert in den Einstellungen mit Migration, die Daten erhält). GPS-Nutzung bestehende Einstellung. Schlüssel `ROUTING_API_KEY` per Build/Secure Storage.

## Offene Fragen
1. Stufen und Standardwert für den Abstand zur Route (Annahme: z. B. 1, 2, 5, 10 km, Standard 2 km).
2. Konkrete Tankerkönig-Vorgaben für Takt und Anzahl der Abfragen (Annahme: Drosselung wie vorhanden, ADR legt Abfragenobergrenze und maximale Routenlänge fest).
3. Verhalten bei mehrdeutigen Geokodierungstreffern (Annahme: wie Adresssuche).
4. Ob der Abstand zur Route persistent gespeichert werden darf (reine Einstellung, kein Standort/Adresse) - Annahme ja.
5. Verhalten bei überlanger Route: Abbruch mit Meldung oder Beschränkung auf den Anfangsabschnitt (Annahme: Meldung, keine stille Kürzung).
6. Behandlung der ORS-Nutzungsgrenzen (Kontingent, Fehlercode 429) - Annahme: wie Tankerkönig als vorübergehende Störung mit Wiederholung.
