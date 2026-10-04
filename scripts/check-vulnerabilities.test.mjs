import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import { findVulnerablePackages, formatFindings } from "./check-vulnerabilities.mjs";

const withFindings = JSON.stringify({
  version: 1,
  parameters: "--vulnerable --include-transitive",
  projects: [
    {
      path: "C:/repo/src/vuln.csproj",
      frameworks: [
        {
          framework: "net10.0",
          topLevelPackages: [
            {
              id: "Newtonsoft.Json",
              requestedVersion: "12.0.1",
              resolvedVersion: "12.0.1",
              vulnerabilities: [{ severity: "High", advisoryurl: "https://github.com/advisories/GHSA-5crp-9r3c-p9vr" }],
            },
          ],
          transitivePackages: [
            { id: "Other.Lib", resolvedVersion: "1.0.0", vulnerabilities: [{ severity: "Moderate", advisoryurl: "https://x" }] },
          ],
        },
      ],
    },
  ],
});

const withoutFindings = JSON.stringify({
  version: 1,
  parameters: "--vulnerable --include-transitive",
  sources: ["https://api.nuget.org/v3/index.json"],
  projects: [{ path: "C:/repo/src/ok.csproj", frameworks: [{ framework: "net10.0", topLevelPackages: [{ id: "A", resolvedVersion: "1.0.0" }] }] }],
});

test("findVulnerablePackages erkennt direkte und transitive Funde unabhaengig von der Sprache", () => {
  const findings = findVulnerablePackages(withFindings);
  assert.deepEqual(findings.map((f) => f.id), ["Newtonsoft.Json", "Other.Lib"]);
  assert.equal(findings[0].version, "12.0.1");
  assert.match(formatFindings(findings), /Newtonsoft\.Json 12\.0\.1 .*High .*GHSA-5crp-9r3c-p9vr/);
});

test("findVulnerablePackages liefert ohne Funde eine leere Liste", () => {
  assert.deepEqual(findVulnerablePackages(withoutFindings), []);
  assert.deepEqual(findVulnerablePackages(JSON.stringify({ version: 1, projects: [] })), []);
  assert.deepEqual(findVulnerablePackages(JSON.stringify({ version: 1, projects: [{ path: "x", frameworks: [{ framework: "a", topLevelPackages: [{ id: "A", vulnerabilities: [] }] }] }] })), []);
});

test("findVulnerablePackages meldet ungueltige Ausgabe statt sie als 'ok' zu werten", () => {
  assert.throws(() => findVulnerablePackages("Fuer das Projekt liegen die folgenden anfaelligen Pakete vor."), /kein gueltiges JSON/);
});

test("CLI liefert Exit-Code 1 mit Funden, 0 ohne Funde und 2 bei ungueltigem Inhalt", () => {
  const dir = mkdtempSync(join(tmpdir(), "vuln-test-"));
  try {
    const script = fileURLToPath(new URL("./check-vulnerabilities.mjs", import.meta.url));
    const exitFor = (content) => {
      const file = join(dir, "report.json");
      writeFileSync(file, content);
      return spawnSync(process.execPath, [script, file], { encoding: "utf8" }).status;
    };
    assert.equal(exitFor(withFindings), 1);
    assert.equal(exitFor(withoutFindings), 0);
    assert.equal(exitFor("Für das Projekt liegen die folgenden anfälligen Pakete vor."), 2);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});
