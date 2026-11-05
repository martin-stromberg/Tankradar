// Sprachunabhaengige Pruefung der NuGet-Abhaengigkeiten auf bekannte Sicherheitsluecken.
// Gemeinsame Logik fuer die CI (.github/actions/security-scan) und den lokalen Pruefflauf (scripts/local-ci.ps1).
//
// Aufruf:
//   node scripts/check-vulnerabilities.mjs --run <Solution> [Berichtsdatei]   fuehrt dotnet list aus und wertet aus
//   node scripts/check-vulnerabilities.mjs <JSON-Datei>                       wertet eine vorhandene JSON-Ausgabe aus
// Exit-Code: 0 = keine Funde, 1 = anfaellige Pakete gefunden, 2 = Aufruf-/Ausgabefehler (zaehlt ebenfalls als Fehlschlag).
//
// Ausgewertet wird ausschliesslich das JSON (`--format json`, Eintraege unter `vulnerabilities`), nie lokalisierter Text.
import { spawnSync } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";
import { pathToFileURL } from "node:url";

/** Sammelt alle Pakete mit mindestens einem Eintrag unter `vulnerabilities` aus der JSON-Ausgabe von `dotnet list package`. */
export function findVulnerablePackages(jsonText) {
  let report;
  try {
    report = JSON.parse(jsonText);
  } catch (error) {
    throw new Error(`Ausgabe von 'dotnet list package' ist kein gueltiges JSON: ${error.message}`);
  }
  const findings = [];
  for (const project of report.projects ?? []) {
    for (const framework of project.frameworks ?? []) {
      for (const list of [framework.topLevelPackages, framework.transitivePackages]) {
        for (const pkg of list ?? []) {
          if (Array.isArray(pkg.vulnerabilities) && pkg.vulnerabilities.length > 0) {
            findings.push({
              project: project.path,
              framework: framework.framework,
              id: pkg.id,
              version: pkg.resolvedVersion ?? pkg.requestedVersion,
              vulnerabilities: pkg.vulnerabilities,
            });
          }
        }
      }
    }
  }
  return findings;
}

/** Menschenlesbare Zusammenfassung der Funde. */
export function formatFindings(findings) {
  return findings
    .map((f) => {
      const details = f.vulnerabilities.map((v) => `${v.severity ?? "?"} ${v.advisoryurl ?? ""}`.trim()).join("; ");
      return `${f.id} ${f.version} (${f.framework}, ${f.project}): ${details}`;
    })
    .join("\n");
}

function evaluate(jsonText) {
  const findings = findVulnerablePackages(jsonText);
  if (findings.length === 0) {
    console.log("Keine anfaelligen Pakete gefunden.");
    return 0;
  }
  console.error(`Anfaellige Pakete gefunden (${findings.length}):\n${formatFindings(findings)}`);
  return 1;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const args = process.argv.slice(2);
  try {
    if (args[0] === "--run") {
      const [, solution, reportPath = "vulnerable-packages.json"] = args;
      if (!solution) throw new Error("Usage: node scripts/check-vulnerabilities.mjs --run <Solution> [Berichtsdatei]");
      // Englische CLI-Ausgabe erzwingen (nur fuer Fehlermeldungen/Warnungen relevant, das JSON ist ohnehin sprachneutral).
      const run = spawnSync(
        "dotnet",
        ["list", solution, "package", "--vulnerable", "--include-transitive", "--no-restore", "--format", "json"],
        { encoding: "utf8", maxBuffer: 256 * 1024 * 1024, env: { ...process.env, DOTNET_CLI_UI_LANGUAGE: "en" } },
      );
      if (run.error) throw run.error;
      if (run.stderr) process.stderr.write(run.stderr);
      if (run.status !== 0) throw new Error(`'dotnet list package' endete mit Exit-Code ${run.status}.\n${run.stdout}`);
      writeFileSync(reportPath, run.stdout);
      process.exit(evaluate(run.stdout));
    } else if (args[0]) {
      process.exit(evaluate(readFileSync(args[0], "utf8")));
    } else {
      throw new Error("Usage: node scripts/check-vulnerabilities.mjs (--run <Solution> [Berichtsdatei] | <JSON-Datei>)");
    }
  } catch (error) {
    console.error(error.message);
    process.exit(2);
  }
}
