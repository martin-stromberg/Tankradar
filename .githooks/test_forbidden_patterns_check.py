#!/usr/bin/env python3
"""Unit-Tests für forbidden-patterns-check.py.

Läuft mit der Standard-Library (unittest), keine externen Abhängigkeiten
nötig. Aufruf: python3 .githooks/test_forbidden_patterns_check.py
"""
import importlib.util
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).parent / 'forbidden-patterns-check.py'


def _load_module():
    spec = importlib.util.spec_from_file_location('forbidden_patterns_check', MODULE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


fpc = _load_module()


class ForbiddenPatternsCheckTests_ApiKeys(unittest.TestCase):
    def test_forbidden_patterns_detects_api_key(self):
        # Bewusst über String-Verkettung zusammengesetzt, statt als zusammenhaengendes
        # Literal im Quelltext zu stehen: Sonst wuerde forbidden-patterns-check.py bei
        # einem --all-Lauf diese Testdatei selbst als Fund melden.
        key_name = 'FUEL' + '_API_KEY'
        content = 'var key = "' + key_name + '=sk_live_abc123";'
        issues = fpc.check_api_keys('config.cs', content)
        self.assertTrue(any('API-Schlüssel' in issue for issue in issues))

    def test_forbidden_patterns_ignores_environment_variable_reference(self):
        issues = fpc.check_api_keys('deploy.ps1', 'ApiToken = $env:VIDEOWEBPLAYER_MAUI_API_TOKEN,')
        self.assertEqual(issues, [])


class ForbiddenPatternsCheckTests_Files(unittest.TestCase):
    def test_forbidden_patterns_detects_certificate(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'signing.pfx').write_bytes(b'dummy')
            issues = fpc.check_file(root, 'signing.pfx')
            self.assertTrue(any('.pfx' in issue for issue in issues))

    def test_forbidden_patterns_detects_db_dump(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'dump.sqlite').write_bytes(b'dummy')
            issues = fpc.check_file(root, 'dump.sqlite')
            self.assertTrue(any('Datenbank-Dump' in issue for issue in issues))

    def test_forbidden_patterns_detects_large_log_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            big_log = root / 'app.log'
            big_log.write_bytes(b'x' * (1024 * 1024 + 1))
            issues = fpc.check_file(root, 'app.log')
            self.assertTrue(any('Logdatei zu groß' in issue for issue in issues))

    def test_forbidden_patterns_ignores_small_log_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'app.log').write_bytes(b'x' * 100)
            issues = fpc.check_file(root, 'app.log')
            self.assertEqual(issues, [])


if __name__ == '__main__':
    unittest.main()
