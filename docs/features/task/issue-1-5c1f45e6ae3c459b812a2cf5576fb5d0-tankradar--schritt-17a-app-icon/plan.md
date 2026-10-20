# Plan Schritt 17a
1. appicon.svg: vollflächiger Verlauf (userSpaceOnUse, keine Rundung/Transparenz). Der Resizetizer rendert linearGradient (Vorschau geprüft), daher keine Ersatzlösung nötig.
2. appiconfg.svg: Motiv aus dem Entwurf, skaliert (Faktor 6) und zentriert, Strichstärken leicht erhöht für 16-32 px.
3. splash.svg: dasselbe Motiv; Farbe liefert MauiSplashScreen (csproj unverändert).
4. Test AppIconResourceTests (Quelldateien + csproj-Einbindung); create-review-version.ps1 prüft erzeugte Icon-Dateien in der Build-Ausgabe.
5. Doku: project-plan unverändert (Schritt 17a bereits beschrieben), changes.log, Hilfe/README bei Bedarf.

## Offene Punkte
Keine.
