import assert from "node:assert/strict";
import { test } from "node:test";
import {
  classifyWorkflowRef,
  parseManualTag,
  resolveReleaseVersion
} from "./resolve-release-version.mjs";

const tagMissing = () => false;

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

test("manuell: gepushter Tag wird ohne Existenzprüfung released", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "tag", refName: "v0.3.0" },
    determineVersion: () => assert.fail("darf nicht aufgerufen werden"),
    tagExists: () => assert.fail("darf nicht aufgerufen werden")
  });
  assert.deepEqual(result, {
    released: "true", reason: "new-release", version: "0.3.0", tag: "v0.3.0", release_kind: "manual"
  });
});

test("automatisch: Ziel-Tag fehlt -> new-release", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "branch", refName: "main" },
    determineVersion: () => ({ changed: "true", version: "0.1.0" }),
    tagExists: tagMissing
  });
  assert.deepEqual(result, {
    released: "true", reason: "new-release", version: "0.1.0", tag: "v0.1.0", release_kind: "automatic"
  });
});

test("automatisch: Ziel-Tag vorhanden -> already-released", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "branch", refName: "main" },
    determineVersion: () => ({ changed: "true", version: "0.1.0" }),
    tagExists: () => true
  });
  assert.equal(result.released, "false");
  assert.equal(result.reason, "already-released");
  assert.equal(result.tag, "v0.1.0");
  assert.equal(result.version, "0.1.0");
});

test("automatisch: ohne releasefähige Commits -> no-releasable-commits (keine Tag-Prüfung)", async () => {
  const result = await resolveReleaseVersion({
    ref: { refType: "branch", refName: "main" },
    determineVersion: () => ({ changed: "false", version: "" }),
    tagExists: () => assert.fail("darf nicht aufgerufen werden")
  });
  assert.equal(result.released, "false");
  assert.equal(result.reason, "no-releasable-commits");
});
