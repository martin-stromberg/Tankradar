# Plan-Check — Aufruf der Tankstellendetails (Issue #44)

**Status: Plan vollständig**

## Geprüfte Dimensionen (Kreuzverhör)

### Annahmen

- **„`Opacity=0.01` liegt unter der iOS-`hitTest`-Schwelle“** — plausibel (`float` 0.01f ≈ 0.0099999998 < 0.01; Apple-Doku: `alpha < 0.01` wird ignoriert), aber nicht auf dem Gerät verifiziert. **Tragfähig trotzdem:** Der Fix (Wiederherstellung des `TapGestureRecognizer`) ist unabhängig vom exakten Plattformmechanismus korrekt — er stellt die dokumentierte Doppelmechanik (`docs/help/Startseite/ablauf-technisch.md:40`) wieder her, die in `StationBottomSheet` nachweislich funktioniert. Empirisch belegt: Der Fehler existiert seit `0506c99` (Entfernung des Recognizers bei gleichzeitigem Verbleib im BottomSheet).
- **„Doppelauslösung ist abgesichert“** — belegt: `_isOpeningStation` in `FavoritesViewModel`, `NearbySectionViewModel`, `FavoriteGroupViewModel`; Zustand vor `0506c99` (Recognizer + Schaltfläche) war derselbe Aufbau.
- **„Windows/Android funktionieren ohnehin“** — unter Windows empirisch verifiziert (echter `Mouse.Click` auf Overlay-`Button` öffnet Details). Dort ändert der Recognizer nichts am Verhalten.

### Entscheidungen

- **Recognizer statt Opacity-Anhebung (z. B. 0.02) oder `InputTransparent`-Umbau:** begründet — Recognizer ist das dokumentierte, im BottomSheet bewährte Muster; Opacity-Herumrechnen wäre fragil; `InputTransparent` auf dem Button entfernt die UIA-/Bedienhilfen-Bedienung (AutomationId `{Prefix}.Open` muss Invoke bleiben).
- **`FavoriteGroupPage` im Scope:** gleiche Overlay-only-Konstruktion am Stationsnamen, gleicher Defekt, beabsichtigtes Bedienkonzept laut `changes.log` („über den Tankstellennamen“), selbe `_isOpeningStation`-Sperre. Geringes Risiko, vermeidet gesicherten Folgefehler.
- **Struktureller XAML-Test statt echter Eingabe-E2E:** Off-Screen-Fensterbetrieb verbietet Mausklicks in der CI (bewusste Konvention); der Strukturtest sichert genau den Regressionsschritt von `0506c99` ab. Invoke-E2E-Abdeckung des Fachflusses existiert für beide Bereiche.

### Risiken / Randfälle

- **Recognizer stört Scrollen/Wischen:** Historisch bereits so umgesetzt (vor `0506c99`), kein neuer Eingriff; `TapGestureRecognizer` konkurriert nicht mit Scrollgesten.
- **Recognizer auf Namens-`Grid` in `FavoriteGroupPage` feuert bei Klick auf Edit-Elemente:** Nein — das `Grid` umfasst nur Namenszeile und Overlay; Prioritäts-Chips und Bearbeiten-Schaltflächen liegen außerhalb.
- **Verbleibende Plattformlücke (iOS-Echtabnahme):** dokumentiert; Anwender-Abnahme per TestFlight, strukturelle Absicherung im Unit-Test.

### Vollständigkeit

- `changes.log`-Eintrag statt `docs/RELEASE_NOTES.md` (nicht vorhanden; `changes.log` ist das Änderungsprotokoll).
- `docs/help/Startseite/ablauf-technisch.md:40` beschreibt den Zustand bereits korrekt — nach dem Fix stimmen Doku und Code wieder überein; `ablauf-anwender.md` bleibt zutreffend (Kachel antippbar).
- Kein Datenmodell-, Schnittstellen- oder Testinfrastruktur-Eingriff nötig.

## Ergebnis

Keine kritischen Probleme. Der Plan deckt beide Akzeptanzpfade der Anforderung, sichert den Regressionsmechanismus strukturell ab und begrenzt den Scope begründet.
