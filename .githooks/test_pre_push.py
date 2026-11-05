#!/usr/bin/env python3
"""Tests für die Commit-Bereich-Auswertung des pre-push-Hooks.

Startet den Hook per 'sh' gegen ein Temp-Repo mit synthetischem stdin. Über
PRE_PUSH_COMMIT_CHECK_ONLY=1 endet der Hook nach der Commit-Prüfung (ohne
Strict-Checks und dotnet test). Ohne 'sh' im PATH werden die Tests übersprungen.
Aufruf: python3 .githooks/test_pre_push.py
"""
import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

HOOKS_DIR = Path(__file__).parent
SH = shutil.which('sh')
ZERO = '0' * 40


@unittest.skipIf(SH is None, "'sh' nicht im PATH")
class PrePushHookTests_CommitRange(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.repo = self._tmp.name
        self._git('init', '-q', '-b', 'main')
        self._git('config', 'user.email', 'test@example.com')
        self._git('config', 'user.name', 'Test')
        self._commit('a.txt', 'chore: initialer Commit für Testrepo')
        self.main_sha = self._git('rev-parse', 'HEAD')
        self._git('checkout', '-q', '-b', 'feature/test')

    def tearDown(self):
        self._tmp.cleanup()

    def _git(self, *args):
        return subprocess.run(['git', *args], cwd=self.repo, capture_output=True, text=True,
                              check=True).stdout.strip()

    def _commit(self, name, message):
        (Path(self.repo) / name).write_text(name)
        self._git('add', name)
        self._git('commit', '-q', '-m', message)
        return self._git('rev-parse', 'HEAD')

    def _push(self, stdin):
        env = dict(os.environ, PRE_PUSH_COMMIT_CHECK_ONLY='1')
        env.pop('SKIP_HOOKS', None)
        # Binär übergeben: im Textmodus würde Windows LF zu CRLF umwandeln.
        res = subprocess.run(
            [SH, (HOOKS_DIR / 'pre-push').as_posix(), 'origin', 'url'], cwd=self.repo,
            input=stdin.encode('utf-8'), capture_output=True, env=env,
        )
        res.stdout = res.stdout.decode('utf-8', errors='replace')
        res.stderr = res.stderr.decode('utf-8', errors='replace')
        return res

    def test_new_branch_with_valid_commit_passes(self):
        sha = self._commit('b.txt', 'feat(core): gültiger Commit auf neuem Branch')
        res = self._push(f'refs/heads/feature/test {sha} refs/heads/feature/test {ZERO}\n')
        self.assertEqual(res.returncode, 0, res.stdout + res.stderr)

    def test_new_branch_with_invalid_commit_fails(self):
        sha = self._commit('b.txt', 'ungültige Nachricht ohne Typ')
        res = self._push(f'refs/heads/feature/test {sha} refs/heads/feature/test {ZERO}\n')
        self.assertEqual(res.returncode, 1, res.stdout + res.stderr)

    def test_existing_branch_checks_only_pushed_range(self):
        old = self._commit('b.txt', 'ungültige alte Nachricht ohne Typ')
        sha = self._commit('c.txt', 'feat(core): gültiger neuer Commit im Bereich')
        res = self._push(f'refs/heads/feature/test {sha} refs/heads/feature/test {old}\n')
        self.assertEqual(res.returncode, 0, res.stdout + res.stderr)

    def test_existing_branch_with_invalid_new_commit_fails(self):
        old = self._commit('b.txt', 'feat(core): gültiger alter Commit')
        sha = self._commit('c.txt', 'ungültiger neuer Commit ohne Typ')
        res = self._push(f'refs/heads/feature/test {sha} refs/heads/feature/test {old}\n')
        self.assertEqual(res.returncode, 1, res.stdout + res.stderr)

    def test_unknown_remote_sha_does_not_bypass_check(self):
        sha = self._commit('b.txt', 'ungültige Nachricht ohne Typ')
        res = self._push(f'refs/heads/feature/test {sha} refs/heads/feature/test {"1" * 40}\n')
        self.assertEqual(res.returncode, 1, res.stdout + res.stderr)

    def test_branch_deletion_passes_without_check(self):
        res = self._push(f'(delete) {ZERO} refs/heads/feature/test {self.main_sha}\n')
        self.assertEqual(res.returncode, 0, res.stdout + res.stderr)

    def test_push_to_main_is_blocked(self):
        sha = self._commit('b.txt', 'feat(core): gültiger Commit für main')
        res = self._push(f'refs/heads/feature/test {sha} refs/heads/main {self.main_sha}\n')
        self.assertEqual(res.returncode, 1, res.stdout + res.stderr)

    def test_empty_stdin_passes(self):
        res = self._push('')
        self.assertEqual(res.returncode, 0, res.stdout + res.stderr)


if __name__ == '__main__':
    unittest.main()
