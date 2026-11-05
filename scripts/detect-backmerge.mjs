#!/usr/bin/env node
// Erkennt, ob ein Commit (PR-Head bzw. Push-Spitze) eine reine Rückführung von main nach staging ist.
//
// Wichtig: Bei pull_request-Events ist HEAD der von GitHub erzeugte Merge-Commit (refs/pull/N/merge), dessen
// erster Parent die Spitze des Basisbranches ist. Deshalb wird immer der explizit übergebene Head-Commit
// ausgewertet (github.event.pull_request.head.sha bzw. github.sha), und nur Parents NACH dem ersten zählen:
// Der erste Parent ist die Linie, in die gemergt wird, und kann zufällig main sein (staging == main).
//
// Back-Merge, wenn
//   (1) der Head ein Merge-Commit ist, dessen weiterer Parent (2., 3., ...) die main-Spitze ist, oder
//   (2) der Inhalt (Tree) des Heads identisch zu main ist.
//
// Aufruf: node scripts/detect-backmerge.mjs <head-sha> <main-sha>   -> gibt "true" oder "false" aus.
import { execFileSync } from "node:child_process";
import { pathToFileURL } from "node:url";

function git(args, cwd) {
  return execFileSync("git", args, { cwd, encoding: "utf8" }).trim();
}

/** @returns {{ isBackmerge: boolean, reason: "main-merge" | "identical-tree" | "none" }} */
export function detectBackmerge(headSha, mainSha, cwd = process.cwd()) {
  const head = git(["rev-parse", "--verify", `${headSha}^{commit}`], cwd);
  const main = git(["rev-parse", "--verify", `${mainSha}^{commit}`], cwd);

  const parents = git(["show", "-s", "--format=%P", head], cwd).split(/\s+/).filter(Boolean);
  if (parents.slice(1).includes(main)) {
    return { isBackmerge: true, reason: "main-merge" };
  }

  const headTree = git(["rev-parse", `${head}^{tree}`], cwd);
  const mainTree = git(["rev-parse", `${main}^{tree}`], cwd);
  if (headTree === mainTree) {
    return { isBackmerge: true, reason: "identical-tree" };
  }

  return { isBackmerge: false, reason: "none" };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const [headSha, mainSha] = process.argv.slice(2);
  if (!headSha || !mainSha) {
    console.error("Usage: node scripts/detect-backmerge.mjs <head-sha> <main-sha>");
    process.exit(2);
  }
  const { isBackmerge, reason } = detectBackmerge(headSha, mainSha);
  console.error(`reason=${reason}`);
  console.log(isBackmerge ? "true" : "false");
}
