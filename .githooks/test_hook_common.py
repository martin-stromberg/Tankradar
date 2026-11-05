#!/usr/bin/env python3
"""Unit-Tests für _hook_common.py.

Läuft mit der Standard-Library (unittest), keine externen Abhängigkeiten
nötig. Aufruf: python3 .githooks/test_hook_common.py
"""
import contextlib
import importlib.util
import io
import os
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).parent / '_hook_common.py'


def _load_module():
    spec = importlib.util.spec_from_file_location('hook_common', MODULE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class _FakeCompletedProcess:
    def __init__(self, returncode=0, stdout=''):
        self.returncode = returncode
        self.stdout = stdout


class HookCommonTests_Run(unittest.TestCase):
    def setUp(self):
        self.module = _load_module()

    def test_run_executes_command_and_returns_completed_process(self):
        result = self.module.run('git', '--version')
        self.assertEqual(result.returncode, 0)
        self.assertIn('git version', result.stdout)

    def test_run_reports_failure_via_returncode(self):
        result = self.module.run('git', 'this-is-not-a-git-subcommand')
        self.assertNotEqual(result.returncode, 0)


class HookCommonTests_RepoRoot(unittest.TestCase):
    def setUp(self):
        self.module = _load_module()
        self.original_cwd = os.getcwd()

    def tearDown(self):
        os.chdir(self.original_cwd)

    def test_repo_root_returns_path_inside_git_repository(self):
        # Der Testlauf selbst findet innerhalb des Tankradar-Repos statt.
        root = self.module.repo_root()
        self.assertIsNotNone(root)
        self.assertTrue((root / '.git').exists())

    def test_repo_root_returns_none_outside_git_repository(self):
        with tempfile.TemporaryDirectory() as tmp:
            os.chdir(tmp)
            try:
                stderr_capture = io.StringIO()
                with contextlib.redirect_stderr(stderr_capture):
                    root = self.module.repo_root()
            finally:
                # Vor dem Verlassen des 'with'-Blocks zurückwechseln, sonst
                # schlägt die Bereinigung des Temp-Verzeichnisses unter
                # Windows fehl (Verzeichnis ist "in Benutzung", da es das
                # aktuelle Arbeitsverzeichnis des Prozesses ist).
                os.chdir(self.original_cwd)
            self.assertIsNone(root)
            self.assertIn('not inside a git repository', stderr_capture.getvalue())


class HookCommonTests_StagedFiles(unittest.TestCase):
    def setUp(self):
        self.module = _load_module()
        self.original_run = self.module.run

    def tearDown(self):
        self.module.run = self.original_run

    def test_staged_files_parses_git_output_and_skips_blank_lines(self):
        self.module.run = lambda *args: _FakeCompletedProcess(0, 'a.txt\nsrc/b.cs\n\n')
        self.assertEqual(self.module.staged_files(), ['a.txt', 'src/b.cs'])

    def test_staged_files_returns_empty_list_when_git_command_fails(self):
        self.module.run = lambda *args: _FakeCompletedProcess(1, '')
        self.assertEqual(self.module.staged_files(), [])

    def test_staged_files_returns_empty_list_when_nothing_staged(self):
        self.module.run = lambda *args: _FakeCompletedProcess(0, '')
        self.assertEqual(self.module.staged_files(), [])


class HookCommonTests_FindSolution(unittest.TestCase):
    def setUp(self):
        self.module = _load_module()

    def test_find_solution_returns_none_when_no_solution_file_exists(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertIsNone(self.module.find_solution(Path(tmp)))

    def test_find_solution_finds_sln_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'Tankradar.sln').write_text('')
            result = self.module.find_solution(root)
            self.assertEqual(result, root / 'Tankradar.sln')

    def test_find_solution_finds_slnx_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'Tankradar.slnx').write_text('')
            result = self.module.find_solution(root)
            self.assertEqual(result, root / 'Tankradar.slnx')

    def test_find_solution_returns_first_match_alphabetically_when_multiple_exist(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'Zeta.sln').write_text('')
            (root / 'Alpha.slnx').write_text('')
            result = self.module.find_solution(root)
            self.assertEqual(result, root / 'Alpha.slnx')


if __name__ == '__main__':
    unittest.main()
