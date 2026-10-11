# ADR 0007: Startseite – Abweichungen vom Designentwurf und Entscheidungen

## Status

Angenommen (2026-10-11)

## Kontext

Schritt 11 baut den Bereich „Favoriten“ zur Startseite aus. Verbindlich ist der Designentwurf
(`design-draft/stitch_smart_fuel_charge_tracker.zip`, Screen `startseite_favoriten_gruppen`) sowie die Anforderung des
Schritts: je Favoritengruppe die Tankstellen mit Name, Entfernung (nur bei erlaubtem Standort), Preisen der gewählten
Sorten in deren Reihenfolge mit Alter („vor X Min.“, Amber ab 60 Minuten) und Hinweisen; Aktualisierung beim Öffnen und
auf Wunsch; Offline-Stand mit Hinweis; automatische Aktualisierung nach Wiederverbindung; optional der Bereich „In der
Nähe“. Der Entwurf enthält darüber hinaus Elemente ohne Entsprechung in der Anforderung.

## Entscheidung

Folgende Elemente des Entwurfs sind bewusst nicht umgesetzt:

- **Keine Tages-Empfehlung** („günstigste Tankstelle heute“ o. ä.): Die Anforderung nennt sie nicht; eine Empfehlung
  setzt eine Bewertungslogik (Preis, Entfernung, Weg) voraus, die nicht festgelegt ist.
- **Keine Routenknöpfe** („Route starten“ je Tankstelle): Die Navigation zur Tankstelle gehört in die Detailansicht
  ([ADR 0004](0004-station-detail-design-deviations.md)). Die Kachel öffnet die Detailansicht (Antippen oder „Details öffnen“).
- **Keine Strom-/Ladestations-Elemente:** Strompreise sind in Version 1 ausgeschlossen ([ADR 0001](0001-no-electricity-prices-in-v1.md)).
- **Kein Marken-Logo** an der Kachel: Die Quelle liefert keine Logos; Logos Dritter werden nicht eingebunden.
  Platzhalter-Symbole würden eine Marke vortäuschen, die Kachel zeigt den Namen der Tankstelle als Text.
- **Kein Tiefstpreis-Badge** („Günstigster“, „Nah“) an der Kachel: Ein Vergleich über Tankstellen und Sorten ist in der
  Anforderung nicht vorgesehen; angezeigt wird nur, was die Quelle liefert (Entscheidung „Fehlende Quelldaten“).
- **Keine Gruppen-Mindestpreisangabe** im Gruppenkopf: Der Kopf zeigt Name, Beschreibung und Anzahl der Tankstellen. Ein
  Mindestpreis je Gruppe wäre sortenabhängig und könnte veraltete Preise hervorheben.
- **Keine Kopfzeile „Vor X Min. aktualisiert (Quelle)“:** Das Alter steht bei jedem Preis an der Kachel („vor X Min.“, Amber
  ab 60 Minuten); eine Gesamtangabe mit Quelle wäre bei Preisen verschiedenen Alters irreführend. Die Kopfzeile zeigt nur den
  Offline-Hinweis.
- **Keine Gruppensymbole und keine Zwischenüberschrift „Meine Favoriten · N Gruppen“:** Die Anforderung sieht für Gruppen
  nur Name und Beschreibung vor; der Kopf der Gruppe zeigt Name, Beschreibung und Anzahl der Tankstellen.
- **Bereich „In der Nähe“ ohne Öffnungsstatus und Spar-Zeile:** Die Umkreissuche der Quelle liefert keine Öffnungszeiten
  (der Entwurf zeigt „Geöffnet bis …“), und Aussagen wie „Günstigste in 3 km …“ oder „Bester Preis“ setzen einen
  Preisvergleich voraus, den die Anforderung nicht vorsieht. Der Schalter für den Bereich liegt, wie verlangt, in
  „Optionen“ und nicht im Bereich selbst.
- **Kein Lesezeichen-Knopf je Eintrag in „In der Nähe“:** Der Bereich ist rein informativ, Favoritenaktionen gibt es nur in
  der Detailansicht.
- **Lage des Bereichs „In der Nähe“:** Er steht unter den Favoritengruppen, vor „Neue Gruppe“; der Entwurf ordnet ihn
  anders an.
- **Kein Sortieren der Startseite** und keine Filter für die Favoritengruppen: Die Reihenfolge ist fest (Gruppen nach Namen,
  Tankstellen nach Priorität, dann Name). Spritsorten-Filter gibt es nur im Bereich „In der Nähe“.

## Weitere Entscheidungen

- **Preisabruf nach Kennungen statt Umkreissuche.** Die Preise der Favoriten holt `FuelPriceService.RefreshStationsAsync` über
  den Tankerkönig-Endpunkt `prices.php` (höchstens 10 Kennungen je Anfrage, größere Mengen werden aufgeteilt). Eine Obergrenze
  für die Zahl der Kennungen je Aufruf gibt es nicht, damit keine Favoriten-Tankstelle ohne Preise bleibt; die Last
  begrenzen die Aufteilung, die Drosselung und die Frist von fünf Minuten. Das lokale Lesen (`GetKnownStationsAsync`) kennt
  ohnehin keine Grenze. Das spart Anfragen gegenüber einem Einzelabruf je Favorit und liefert Preise auch für Tankstellen
  außerhalb des Umkreises. Ohne „erzwingen“ werden Tankstellen, die innerhalb der Cache-Lebensdauer erfolgreich
  aktualisiert wurden, nicht erneut abgerufen. Fehler führen zum Rückfall auf die zuletzt bekannten Preise.
- **Lokale Daten zuerst.** Beim Erscheinen zeigt die Seite sofort den lokalen Stand (`GetKnownStationsAsync`), danach folgt
  die Aktualisierung. Ohne Verbindung bleibt es beim lokalen Stand mit Offline-Hinweis in der Kopfzeile; nach
  `IConnectionMonitor.ConnectionRestored` wird nur aktualisiert, wenn zuletzt kein Abruf möglich war oder Preise veraltet
  sind (ab 60 Minuten).
- **Standort nur im Arbeitsspeicher.** Der Standort wird nur ermittelt, wenn die Einstellung ihn erlaubt und ein Bedarf besteht
  (Entfernungen der Favoriten oder „In der Nähe“), nur für die Berechnung gehalten und beim Verlassen der Seite verworfen;
  er wird nie gespeichert oder protokolliert (Protokolle nennen nur Ausnahmetypen). Entfernungen zu Favoriten berechnet die
  App lokal aus Standort und gespeicherter Tankstellenposition. Der Suchcache des Preisdienstes führt die Umkreissuche unter
  einem Schlüssel mit auf drei Nachkommastellen (ca. 110 m) gerundeter Position; diese Schlüssel liegen nur im
  Arbeitsspeicher, und abgelaufene Einträge (nach fünf Minuten) werden bei jeder weiteren Anfrage des Preisdienstes entfernt.
- **„In der Nähe“:** optional (Schalter in „Optionen“, Standard aus, Spalte `ShowNearby` per Migration `AddNearbySetting`),
  sichtbar nur bei aktivem Schalter und Standortnutzung ungleich „Nie“; fester Radius 5 km, höchstens 10 Einträge nach
  Entfernung, Filter „Alle“ und je gewählte Sorte. Rein informativ: kein Zuordnen oder Entfernen von Favoriten; Antippen
  öffnet die Detailansicht, die einzige Stelle für Favoritenaktionen ([ADR 0006](0006-favorite-groups.md)).
- **Gruppen einklappbar** (wie im Entwurf); der Zustand gilt, solange die App läuft, und wird nicht gespeichert.
- **Gruppenverwaltung bleibt auf der Startseite erreichbar** (Neue Gruppe, Gruppe öffnen).

## Folgen

- Zusätzliche Entwurfselemente (Empfehlung, Routen, Logos, Badges) erfordern eigene Anforderungen und gegebenenfalls
  Datenquellen, die die Tankerkönig-API nicht bietet.
- Neue Einstellungen erfordern weiterhin Migrationen, die bestehende Daten erhalten (Skill `entityframework-database`).
