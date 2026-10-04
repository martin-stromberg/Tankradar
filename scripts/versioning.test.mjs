import assert from "node:assert/strict";
import { test } from "node:test";
import {
  applyBump,
  computeNextVersion,
  displayVersion,
  nextRcNumber,
  parseCommit,
  requiredBump
} from "./versioning.mjs";

const commit = (subject, body = "") => ({ subject, body });

test("parseCommit erkennt Typ und Breaking-Kennzeichen", () => {
  assert.deepEqual(parseCommit("feat(ui): neue Karte anzeigen"), { type: "feat", breaking: false });
  assert.deepEqual(parseCommit("feat!: Schnittstelle entfernt"), { type: "feat", breaking: true });
  assert.deepEqual(parseCommit("fix: Absturz", "BREAKING CHANGE: neues Format"), { type: "fix", breaking: true });
  assert.deepEqual(parseCommit("Merge branch 'x'"), { type: null, breaking: false });
});

test("requiredBump wählt die höchste Anhebung", () => {
  assert.equal(requiredBump([commit("docs: nur Doku")]), null);
  assert.equal(requiredBump([commit("fix: Fehler behoben"), commit("chore: aufräumen")]), "patch");
  assert.equal(requiredBump([commit("fix: Fehler behoben"), commit("feat: Funktion neu")]), "minor");
  assert.equal(requiredBump([commit("feat: Funktion neu"), commit("refactor!: Umbau")]), "major");
  assert.equal(requiredBump([commit("perf: schneller")]), "patch");
});

test("erste Version ist 0.1.0", () => {
  const result = computeNextVersion({ latestStableTag: null, commits: [commit("chore: Anfang"), commit("feat: Start")] });
  assert.deepEqual(result, { changed: true, version: "0.1.0", reason: "first-release" });
});

test("ohne Commits und ohne Tag gibt es keine Version", () => {
  assert.equal(computeNextVersion({ latestStableTag: null, commits: [] }).changed, false);
});

test("vor 1.0: Breaking Change und Feature erhöhen nur die Minor-Version", () => {
  assert.equal(computeNextVersion({ latestStableTag: "v0.1.0", commits: [commit("feat!: Umbau")] }).version, "0.2.0");
  assert.equal(computeNextVersion({ latestStableTag: "v0.4.2", commits: [commit("feat: neu")] }).version, "0.5.0");
  assert.equal(computeNextVersion({ latestStableTag: "v0.4.2", commits: [commit("fix: korrigiert")] }).version, "0.4.3");
});

test("niemals automatische Anhebung auf 1.0.0 vor Hauptversion 1", () => {
  for (const tag of ["v0.1.0", "v0.9.0", "v0.99.99"]) {
    const result = computeNextVersion({ latestStableTag: tag, commits: [commit("feat!: Umbau", "BREAKING CHANGE: x")] });
    assert.ok(result.version.startsWith("0."), `${tag} -> ${result.version}`);
  }
});

test("ab 1.0 gilt klassisches SemVer", () => {
  assert.equal(applyBump("1.2.3", "major"), "2.0.0");
  assert.equal(applyBump("1.2.3", "minor"), "1.3.0");
  assert.equal(applyBump("1.2.3", "patch"), "1.2.4");
});

test("ohne releasefähige Commits bleibt die Version unverändert", () => {
  const result = computeNextVersion({ latestStableTag: "v0.1.0", commits: [commit("docs: Hinweis ergänzt")] });
  assert.deepEqual(result, { changed: false, version: "", reason: "no-releasable-commits" });
});

test("ungültiger Tag wird abgelehnt", () => {
  assert.throws(() => computeNextVersion({ latestStableTag: "v0.1.0-rc.1", commits: [commit("feat: x")] }));
});

test("nextRcNumber zählt vorhandene RC-Tags der exakten Version", () => {
  assert.equal(nextRcNumber("0.2.0", []), 1);
  assert.equal(nextRcNumber("0.2.0", ["v0.2.0-rc.1", "v0.2.0-rc.2", "v0.3.0-rc.7", "v0.2.0"]), 3);
  assert.equal(nextRcNumber("0.2.0", ["v0.2.0-rc.4"]), 5);
});

test("displayVersion entfernt das Pre-Release-Suffix", () => {
  assert.equal(displayVersion("0.2.0-rc.3"), "0.2.0");
  assert.equal(displayVersion("0.2.0"), "0.2.0");
});
