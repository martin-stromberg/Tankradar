#!/usr/bin/env python3
"""Unit-Tests für test-execution-check.py.

Läuft mit der Standard-Library (unittest), keine externen Abhängigkeiten
nötig. Aufruf: python3 .githooks/test_test_execution_check.py

`dotnet test` wird für diese Tests nicht wirklich aufgerufen (insbesondere
der Timeout-Test würde sonst unnötig lange dauern): Das Modul wird mit
einer Fake-`subprocess.run`-Funktion isoliert getestet.
"""
import importlib.util
import os
import subprocess
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).parent / 'test-execution-check.py'


def _load_module():
    spec = importlib.util.spec_from_file_location('test_execution_check', MODULE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class _FakeCompletedProcess:
    def __init__(self, returncode=0):
        self.returncode = returncode


class TestExecutionCheckTests_DotnetTest(unittest.TestCase):
    def setUp(self):
        self.module = _load_module()
        self.env_backup = dict(os.environ)
        os.environ.pop('HOOK_SKIP_TESTS', None)
        os.environ.pop('TEST_TIMEOUT_SECONDS', None)
        self.original_subprocess_run = subprocess.run

    def tearDown(self):
        os.environ.clear()
        os.environ.update(self.env_backup)
        subprocess.run = self.original_subprocess_run

    def test_test_execution_check_runs_dotnet_test(self):
        calls = []

        def fake_subprocess_run(args, **kwargs):
            calls.append(args)
            return _FakeCompletedProcess(0)

        with tempfile.TemporaryDirectory() as tmp:
            tmp_root = Path(tmp)
            (tmp_root / 'Test.sln').write_text('')
            self.module.repo_root = lambda: tmp_root
            subprocess.run = fake_subprocess_run

            exit_code = self.module.main()

        self.assertEqual(exit_code, 0)
        self.assertEqual(len(calls), 2)
        self.assertEqual(calls[1][:2], ['dotnet', 'test'])
        self.assertIn('--verbosity', calls[1])

    def test_test_execution_check_builds_once_before_testing_with_no_build(self):
        # Regression: paralleler Build neben laufenden E2E-Tests sperrte Tankradar.MAUI.exe (MSB3027).
        calls = []

        def fake_subprocess_run(args, **kwargs):
            calls.append(args)
            return _FakeCompletedProcess(0)

        with tempfile.TemporaryDirectory() as tmp:
            tmp_root = Path(tmp)
            (tmp_root / 'Test.sln').write_text('')
            self.module.repo_root = lambda: tmp_root
            subprocess.run = fake_subprocess_run

            self.module.main()

        self.assertEqual([c[:2] for c in calls], [['dotnet', 'build'], ['dotnet', 'test']])
        self.assertNotIn('--no-build', calls[0])
        self.assertIn('--no-build', calls[1])

    def test_test_execution_check_build_failure_skips_tests(self):
        calls = []

        def fake_subprocess_run(args, **kwargs):
            calls.append(args)
            return _FakeCompletedProcess(1)

        with tempfile.TemporaryDirectory() as tmp:
            tmp_root = Path(tmp)
            (tmp_root / 'Test.sln').write_text('')
            self.module.repo_root = lambda: tmp_root
            subprocess.run = fake_subprocess_run

            exit_code = self.module.main()

        self.assertEqual(exit_code, 1)
        self.assertEqual(len(calls), 1)

    def test_test_execution_check_timeout(self):
        def fake_subprocess_run(args, **kwargs):
            raise subprocess.TimeoutExpired(cmd=args, timeout=kwargs.get('timeout', 1))

        with tempfile.TemporaryDirectory() as tmp:
            tmp_root = Path(tmp)
            (tmp_root / 'Test.sln').write_text('')
            self.module.repo_root = lambda: tmp_root
            subprocess.run = fake_subprocess_run
            os.environ['TEST_TIMEOUT_SECONDS'] = '1'

            exit_code = self.module.main()

        self.assertEqual(exit_code, 1)

    def test_hook_skip_tests_env_var_skips_execution(self):
        os.environ['HOOK_SKIP_TESTS'] = '1'
        exit_code = self.module.main()
        self.assertEqual(exit_code, 0)

    def test_timeout_seconds_respects_override(self):
        os.environ['TEST_TIMEOUT_SECONDS'] = '42'
        self.assertEqual(self.module.timeout_seconds(), 42)

    def test_timeout_seconds_default(self):
        self.assertEqual(self.module.timeout_seconds(), self.module.DEFAULT_TIMEOUT_SECONDS)


if __name__ == '__main__':
    unittest.main()
