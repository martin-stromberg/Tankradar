#!/usr/bin/env python3
"""Unit-Tests für conventional-commits-check.py.

Läuft mit der Standard-Library (unittest), keine externen Abhängigkeiten
nötig. Aufruf: python3 .githooks/test_conventional_commits_check.py
"""
import importlib.util
import os
import subprocess
import sys
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

    def test_conventional_commits_breaking_change_notation_feat(self):
        self.assertEqual(ccc.validate_message('feat!: entfernt veraltete Konfigurationsoption'), [])

    def test_conventional_commits_breaking_change_notation_fix(self):
        self.assertEqual(ccc.validate_message('fix(core)!: ändert das Antwortformat der API'), [])

    def test_conventional_commits_allows_ci_type(self):
        self.assertEqual(ccc.validate_message('ci: aktualisiert die Build-Pipeline-Konfiguration'), [])

    def test_conventional_commits_allows_build_type(self):
        self.assertEqual(ccc.validate_message('build: aktualisiert die Paketabhängigkeiten'), [])

    def test_conventional_commits_allows_style_type(self):
        self.assertEqual(ccc.validate_message('style: passt die Einrückung im Quelltext an'), [])

    def test_conventional_commits_allows_revert_type(self):
        self.assertEqual(ccc.validate_message('revert: macht den vorherigen Commit rückgängig'), [])

    def test_conventional_commits_rejects_breaking_marker_inside_scope(self):
        errors = ccc.validate_message('feat(scope!): ungültige Position des Breaking-Markers')
        self.assertTrue(len(errors) >= 1)

    def test_conventional_commits_rejects_breaking_marker_after_space(self):
        errors = ccc.validate_message('feat !: ungültige Position des Breaking-Markers')
        self.assertTrue(len(errors) >= 1)


class ConventionalCommitsCheckTests_BreakingChange(unittest.TestCase):
    """Prüft die Erkennung von 'BREAKING CHANGE:' im Commit-Body (Runde 2, Schritt 2)."""

    def test_conventional_commits_breaking_change_in_body(self):
        body = 'Einige Details zur Änderung.\n\nBREAKING CHANGE: entfernt den alten Endpunkt.'
        self.assertTrue(ccc.check_breaking_change(body))

    def test_conventional_commits_no_breaking_change_in_body(self):
        body = 'Einige Details zur Änderung ohne Breaking Change.'
        self.assertFalse(ccc.check_breaking_change(body))

    def test_conventional_commits_has_breaking_notation(self):
        self.assertTrue(ccc.has_breaking_notation('feat(core)!: ändert das Antwortformat'))
        self.assertFalse(ccc.has_breaking_notation('feat(core): ändert das Antwortformat'))


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

        subjects = [subject for _, subject, _ in commits]
        self.assertEqual(subjects, ['feat(core): fügt Testdatei hinzu'])

    def test_commits_since_divergence_respects_explicit_end_ref(self):
        """Der pre-push-Hook übergibt den tatsächlich gepushten Bereich statt HEAD (Runde 2)."""
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
            self._run_git(tmp, 'commit', '-q', '-m', 'feat(core): erster gepushter Commit')
            first_pushed_sha = subprocess.run(
                ['git', 'rev-parse', 'HEAD'], cwd=tmp, capture_output=True, text=True, check=True,
            ).stdout.strip()
            (Path(tmp) / 'c.txt').write_text('c')
            self._run_git(tmp, 'add', 'c.txt')
            self._run_git(tmp, 'commit', '-q', '-m', 'feat(core): zweiter, noch nicht gepushter Commit')

            cwd = os.getcwd()
            try:
                os.chdir(tmp)
                commits = ccc.commits_since_divergence('main', first_pushed_sha)
            finally:
                os.chdir(cwd)

        subjects = [subject for _, subject, _ in commits]
        self.assertEqual(subjects, ['feat(core): erster gepushter Commit'])

    def test_commits_since_divergence_reads_body(self):
        with tempfile.TemporaryDirectory() as tmp:
            self._run_git(tmp, 'init', '-q', '-b', 'main')
            self._run_git(tmp, 'config', 'user.email', 'test@example.com')
            self._run_git(tmp, 'config', 'user.name', 'Test')
            (Path(tmp) / 'a.txt').write_text('a')
            self._run_git(tmp, 'add', 'a.txt')
            self._run_git(tmp, 'commit', '-q', '-m', 'chore: initialer Commit für Testrepo')
            (Path(tmp) / 'b.txt').write_text('b')
            self._run_git(tmp, 'add', 'b.txt')
            self._run_git(tmp, 'commit', '-q', '-m', 'feat!: entfernt alte Option',
                          '-m', 'BREAKING CHANGE: Option entfernt.')
            base = subprocess.run(['git', 'rev-parse', 'HEAD~1'], cwd=tmp, capture_output=True,
                                  text=True, check=True).stdout.strip()

            cwd = os.getcwd()
            try:
                os.chdir(tmp)
                commits = ccc.commits_since_divergence(base)
            finally:
                os.chdir(cwd)

        self.assertEqual(len(commits), 1)
        self.assertTrue(ccc.check_breaking_change(commits[0][2]))


class ConventionalCommitsCheckTests_UnresolvableRefs(unittest.TestCase):
    """Nicht auflösbare Referenzen dürfen die Prüfung nicht still umgehen."""

    def _git(self, cwd, *args):
        subprocess.run(['git', *args], cwd=cwd, capture_output=True, text=True, check=True)

    def _make_repo(self, tmp, last_subject):
        self._git(tmp, 'init', '-q', '-b', 'main')
        self._git(tmp, 'config', 'user.email', 'test@example.com')
        self._git(tmp, 'config', 'user.name', 'Test')
        (Path(tmp) / 'a.txt').write_text('a')
        self._git(tmp, 'add', 'a.txt')
        self._git(tmp, 'commit', '-q', '-m', 'chore: initialer Commit für Testrepo')
        self._git(tmp, 'checkout', '-q', '-b', 'feature/test')
        (Path(tmp) / 'b.txt').write_text('b')
        self._git(tmp, 'add', 'b.txt')
        self._git(tmp, 'commit', '-q', '-m', last_subject)

    def _run_script(self, tmp, *args):
        return subprocess.run(
            [sys.executable, str(MODULE_PATH), *args], cwd=tmp, capture_output=True,
            text=True, encoding='utf-8', errors='replace',
        )

    def test_unresolvable_base_falls_back_to_main_and_still_validates(self):
        with tempfile.TemporaryDirectory() as tmp:
            self._make_repo(tmp, 'ungültige Nachricht ohne Typ')
            res = self._run_script(tmp, '--base', '1' * 40, '--end', 'HEAD')
        self.assertEqual(res.returncode, 1)
        self.assertIn('Fallback', res.stderr)

    def test_unresolvable_end_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            self._make_repo(tmp, 'feat(core): gültige Nachricht im Testrepo')
            res = self._run_script(tmp, '--end', '2' * 40)
        self.assertEqual(res.returncode, 1)

    def test_breaking_notation_without_footer_only_hints(self):
        with tempfile.TemporaryDirectory() as tmp:
            self._make_repo(tmp, 'feat!: entfernt veraltete Option')
            res = self._run_script(tmp)
        self.assertEqual(res.returncode, 0)
        self.assertIn('HINWEIS', res.stdout)


if __name__ == '__main__':
    unittest.main()
