import assert from "node:assert/strict";
import { test } from "node:test";
import {
  classifyWorkflowRef,
  incompleteReleases,
  parseManualTag,
  releaseHasExpectedAsset,
  resolveReleaseVersion
} from "./resolve-release-version.mjs";

const asset = (name, size = 10, state = "uploaded") => ({ name, size, state });
const complete = (tag, extra = {}) => ({
  tag_name: tag,
  prerelease: false,
  created_at: "2026-01-01T00:00:00Z",
  assets: [asset("release-win-x64.zip"), asset("update.json")],
  ...extra
});

const noReleases = { getRelease: () => null, listReleases: () => [] };

test("parseManualTag akzeptiert vX.Y.Z und lehnt anderes ab", () => {
  assert.equal(parseManualTag("v0.1.0"), "0.1.0");
  assert.equal(parseManualTag("v1.2.3-rc.1"), "1.2.3-rc.1");
  assert.throws(() => parseManualTag("0.1.0"));
  assert.throws(() => parseManualTag("v1.2"));
});

test("classifyWorkflowRef unterscheidet manuell und automatisch", () => {
  assert.equal(classifyWorkflowRef({ refType: "tag", refName: "v0.1.0" }).kind, "manual");
  assert.equal(classifyWorkflowRef({ refType: "branch", refName: "main" }).kind, "automatic");
  assert.throws(() => classifyWorkflowRef({ refType: "branch", refName: "staging" }));
});

test("releaseHasExpectedAsset verlangt alle Assets, hochgeladen und nicht leer", () => {
  assert.equal(releaseHasExpectedAsset(complete("v0.1.0")), true);
  assert.equal(releaseHasExpectedAsset({ assets: [asset("update.json")] }), false);
  assert.equal(releaseHasExpectedAsset({ assets: [asset("release-win-x64.zip", 0), asset("update.json")] }), false);
  assert.equal(releaseHasExpectedAsset({ assets: [asset("release-win-x64.zip", 5, "starter"), asset("update.json")] }), false);
});

test("Regression 11.1: Pre-Releases werden nie zur Reparatur ausgewählt", () => {
  const rc = { tag_name: "v0.1.0-rc.1", prerelease: true, assets: [] };
  assert.deepEqual(incompleteReleases([rc, complete("v0.1.0")]), []);
});

test("automatisch: neue Version ohne vorhandenes Release wird erstellt", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "branch", refName: "main" },
    determineVersion: () => ({ changed: "true", version: "0.1.0" }),
    ...noReleases
  });
  assert.deepEqual(result, {
    released: "true", reason: "new-release", version: "0.1.0", tag: "v0.1.0", release_kind: "automatic", release_action: "create"
  });
});

test("automatisch: bereits vollständig veröffentlicht -> nichts tun", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "branch", refName: "main" },
    determineVersion: () => ({ changed: "true", version: "0.1.0" }),
    getRelease: () => complete("v0.1.0"),
    listReleases: () => []
  });
  assert.equal(result.released, "false");
  assert.equal(result.reason, "already-released");
});

test("automatisch: vorhandenes Release ohne Asset wird repariert", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "branch", refName: "main" },
    determineVersion: () => ({ changed: "true", version: "0.1.0" }),
    getRelease: () => ({ tag_name: "v0.1.0", assets: [] }),
    listReleases: () => []
  });
  assert.equal(result.release_action, "upload-existing");
  assert.equal(result.tag, "v0.1.0");
});

test("manuell: Tag ohne Release erzeugt ein manuelles Release", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "tag", refName: "v0.3.0" },
    determineVersion: () => assert.fail("darf nicht aufgerufen werden"),
    ...noReleases
  });
  assert.equal(result.release_kind, "manual");
  assert.equal(result.release_action, "create");
  assert.equal(result.version, "0.3.0");
});

test("ohne releasefähige Commits: ältestes unvollständiges Release (kein Pre-Release) wird repariert", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "branch", refName: "main" },
    determineVersion: () => ({ changed: "false", version: "" }),
    getRelease: () => null,
    listReleases: () => [
      { tag_name: "v0.3.0", prerelease: false, created_at: "2026-03-01T00:00:00Z", assets: [] },
      { tag_name: "v0.2.0", prerelease: false, created_at: "2026-02-01T00:00:00Z", assets: [] },
      { tag_name: "v0.1.0-rc.1", prerelease: true, created_at: "2026-01-01T00:00:00Z", assets: [] }
    ]
  });
  assert.equal(result.tag, "v0.2.0");
  assert.equal(result.release_action, "upload-existing");
});

test("ohne releasefähige Commits und ohne unvollständige Releases: nichts zu tun", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "branch", refName: "main" },
    determineVersion: () => ({ changed: "false", version: "" }),
    getRelease: () => null,
    listReleases: () => [complete("v0.1.0")]
  });
  assert.equal(result.released, "false");
  assert.equal(result.reason, "no-releasable-commits");
});
