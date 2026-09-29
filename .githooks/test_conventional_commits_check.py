#!/usr/bin/env python3
"""Unit-Tests für conventional-commits-check.py.

Läuft mit der Standard-Library (unittest), keine externen Abhängigkeiten
nötig. Aufruf: python3 .githooks/test_conventional_commits_check.py
"""
import importlib.util
import os
import subprocess
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).parent / 'conventional-commits-check.py'


def _load_module():
    spec = importlib.util.spec_from_file_location('conventional_commits_check', MODULE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


ccc = _load_module()


class ConventionalCommitsCheckTests_Format(unittest.TestCase):
    def test_conventional_commits_valid_format(self):
        errors = ccc.validate_message('feat(core): fügt eine neue Funktion hinzu')
        self.assertEqual(errors, [])

    def test_conventional_commits_invalid_type(self):
        errors = ccc.validate_message('foo(core): eine ausreichend lange Nachricht')
        self.assertTrue(any('unbekannter Type' in e for e in errors))

    def test_conventional_commits_too_short(self):
        errors = ccc.validate_message('feat(core): kurz')
        self.assertTrue(any('zu kurz' in e for e in errors))

    def test_conventional_commits_allows_plan_and_merge_types(self):
        self.assertEqual(ccc.validate_message('plan: Planungscommit für Entwicklungsschritt 2'), [])
        self.assertEqual(ccc.validate_message('merge: Merge-Commit für Entwicklungsschritt 1'), [])

    def test_conventional_commits_allows_missing_scope(self):
        self.assertEqual(ccc.validate_message('chore: Aufräumarbeiten am Repository'), [])

    def test_conventional_commits_rejects_malformed_message(self):
        errors = ccc.validate_message('das ist keine Conventional-Commit-Nachricht')
        self.assertTrue(len(errors) >= 1)


class ConventionalCommitsCheckTests_GitHistory(unittest.TestCase):
    """Prüft das Auslesen der Commit-Historie seit Divergenz von main (Task 19)."""

    def _run_git(self, cwd, *args):
        subprocess.run(['git', *args], cwd=cwd, capture_output=True, text=True, check=True)

    def test_commits_since_divergence_reads_new_commits(self):
        with tempfile.TemporaryDirectory() as tmp:
            self._run_git(tmp, 'init', '-q', '-b', 'main')
            self._run_git(tmp, 'config', 'user.email', 'test@example.com')
            self._run_git(tmp, 'config', 'user.name', 'Test')
            (Path(tmp) / 'a.txt').write_text('a')
            self._run_git(tmp, 'add', 'a.txt')
            self._run_git(tmp, 'commit', '-q', '-m', 'chore: initialer Commit für Testrepo')
            self._run_git(tmp, 'checkout', '-q', '-b', 'feature/test')
            (Path(tmp) / 'b.txt').write_text('b')
            self._run_git(tmp, 'add', 'b.txt')
            self._run_git(tmp, 'commit', '-q', '-m', 'feat(core): fügt Testdatei hinzu')

            cwd = os.getcwd()
            try:
                os.chdir(tmp)
                commits = ccc.commits_since_divergence('main')
            finally:
                os.chdir(cwd)

        subjects = [subject for _, subject in commits]
        self.assertEqual(subjects, ['feat(core): fügt Testdatei hinzu'])


if __name__ == '__main__':
    unittest.main()
