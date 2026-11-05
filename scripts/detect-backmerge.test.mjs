import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { afterEach, beforeEach, test } from "node:test";
import { detectBackmerge } from "./detect-backmerge.mjs";

let repo;

function git(...args) {
  return execFileSync(
    "git",
    ["-c", "user.name=T", "-c", "user.email=t@example.com", "-c", "commit.gpgsign=false", ...args],
    { cwd: repo, encoding: "utf8" },
  ).trim();
}

function commitFile(name, content) {
  writeFileSync(join(repo, name), content);
  git("add", name);
  git("commit", "-m", `change ${name}`);
  return git("rev-parse", "HEAD");
}

beforeEach(() => {
  repo = mkdtempSync(join(tmpdir(), "backmerge-"));
  git("init", "-q", "-b", "main");
  commitFile("init.txt", "init");
});

afterEach(() => rmSync(repo, { recursive: true, force: true }));

// Simuliert refs/pull/N/merge: Merge-Commit mit staging als erstem Parent und dem Head als zweitem.
function githubMergeCommit(stagingBranch, headSha) {
  git("checkout", "-q", "--detach", stagingBranch);
  git("merge", "--no-ff", "-m", "pr merge", headSha);
  return git("rev-parse", "HEAD");
}

test("Feature-PR bei staging == main ist kein Back-Merge (auch nicht per GitHub-Merge-Commit)", () => {
  git("branch", "staging");
  git("checkout", "-q", "-b", "feature");
  const head = commitFile("feature.txt", "x");
  const main = git("rev-parse", "main");

  const prMerge = githubMergeCommit("staging", head);
  // Der erste Parent des GitHub-Merge-Commits ist main - darf nicht als Back-Merge zählen.
  assert.equal(git("show", "-s", "--format=%P", prMerge).split(" ")[0], main);

  assert.equal(detectBackmerge(head, main, repo).isBackmerge, false);
  // Auch die Push-Spitze nach einem Merge des Features in staging (Parent 1 == main) ist kein Back-Merge.
  assert.equal(detectBackmerge(prMerge, main, repo).isBackmerge, false);
});

test("Feature-PR bei staging vor main ist kein Back-Merge", () => {
  git("checkout", "-q", "-b", "staging");
  commitFile("staging.txt", "s");
  git("checkout", "-q", "-b", "feature");
  const head = commitFile("feature.txt", "x");

  assert.equal(detectBackmerge(head, "main", repo).isBackmerge, false);
});

test("Head ist Merge-Commit von main in staging ist ein Back-Merge", () => {
  git("checkout", "-q", "-b", "staging");
  commitFile("staging.txt", "s");
  git("checkout", "-q", "main");
  commitFile("release.txt", "r");
  const main = git("rev-parse", "main");
  git("checkout", "-q", "staging");
  git("merge", "--no-ff", "-m", "backmerge", "main");
  const head = git("rev-parse", "HEAD");

  assert.deepEqual(detectBackmerge(head, main, repo), { isBackmerge: true, reason: "main-merge" });
});

test("Head mit Inhalt identisch zu main ist ein Back-Merge", () => {
  git("checkout", "-q", "-b", "staging");
  commitFile("staging.txt", "s");
  git("revert", "--no-edit", "HEAD");
  const head = git("rev-parse", "HEAD");
  const main = git("rev-parse", "main");
  assert.notEqual(head, main);

  assert.deepEqual(detectBackmerge(head, main, repo), { isBackmerge: true, reason: "identical-tree" });
});
