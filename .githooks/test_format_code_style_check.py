#!/usr/bin/env python3
"""Unit-Tests für format-code-style-check.py.

Läuft mit der Standard-Library (unittest), keine externen Abhängigkeiten
nötig. Aufruf: python3 .githooks/test_format_code_style_check.py

`dotnet` wird für diese Tests nicht wirklich aufgerufen: Das Modul wird mit
einer Fake-`run`-Funktion isoliert getestet, damit die Tests schnell und
ohne installierte .NET-Toolchain laufen.
"""
import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).parent / 'format-code-style-check.py'


def _load_module():
    spec = importlib.util.spec_from_file_location('format_code_style_check', MODULE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class _FakeResult:
    def __init__(self, returncode=0, stdout='', stderr=''):
        self.returncode = returncode
        self.stdout = stdout
        self.stderr = stderr


class FormatCodeStyleCheckTests_DotnetFormat(unittest.TestCase):
    def setUp(self):
        self.module = _load_module()

    def test_format_code_style_check_calls_dotnet_format(self):
        calls = []
        tmp_root = None

        def fake_run(*args):
            calls.append(args)
            if args[:2] == ('git', 'rev-parse'):
                return _FakeResult(0, stdout=str(tmp_root) + '\n')
            if args[:2] == ('dotnet', '--version'):
                return _FakeResult(0, stdout='10.0.401\n')
            if args[:2] == ('dotnet', 'format'):
                return _FakeResult(0)
            return _FakeResult(1)

        with tempfile.TemporaryDirectory() as tmp:
            tmp_root = Path(tmp)
            (tmp_root / 'Test.sln').write_text('')

            self.module.run = fake_run
            old_argv = sys.argv
            sys.argv = ['format-code-style-check.py']
            try:
                exit_code = self.module.main()
            finally:
                sys.argv = old_argv

        self.assertEqual(exit_code, 0)
        dotnet_format_calls = [c for c in calls if c[:2] == ('dotnet', 'format')]
        self.assertEqual(len(dotnet_format_calls), 1)
        self.assertIn('--verify-no-changes', dotnet_format_calls[0])

    def test_format_code_style_check_warns_but_does_not_block_on_violations(self):
        tmp_root = None

        def fake_run(*args):
            if args[:2] == ('git', 'rev-parse'):
                return _FakeResult(0, stdout=str(tmp_root) + '\n')
            if args[:2] == ('dotnet', '--version'):
                return _FakeResult(0, stdout='10.0.401\n')
            if args[:2] == ('dotnet', 'format'):
                return _FakeResult(1, stdout='error WHITESPACE: ...')
            return _FakeResult(1)

        with tempfile.TemporaryDirectory() as tmp:
            tmp_root = Path(tmp)
            (tmp_root / 'Test.sln').write_text('')

            self.module.run = fake_run
            old_argv = sys.argv
            sys.argv = ['format-code-style-check.py']
            try:
                exit_code = self.module.main()
            finally:
                sys.argv = old_argv

        self.assertEqual(exit_code, 0)


if __name__ == '__main__':
    unittest.main()
