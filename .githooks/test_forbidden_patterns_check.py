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

    def test_forbidden_patterns_detects_api_key_json_notation(self):
        # Bewusst über String-Verkettung zusammengesetzt (siehe Kommentar oben in dieser
        # Klasse), damit forbidden-patterns-check.py diese Testdatei bei einem --all-Lauf
        # nicht selbst als Fund meldet.
        field_name = 'Tankerkoenig' + 'ApiKey'
        content = '{"' + field_name + '": "sk_live_abc123"}'
        issues = fpc.check_api_keys('appsettings.json', content)
        self.assertTrue(any('JSON-Notation' in issue for issue in issues))

    def test_forbidden_patterns_detects_generic_json_api_keys(self):
        field_name = 'Client' + 'Secret'
        content = '{"' + field_name + '": "s3cr3t-value"}'
        issues = fpc.check_api_keys('appsettings.json', content)
        self.assertTrue(any('JSON-Notation' in issue for issue in issues))

    def test_forbidden_patterns_ignores_json_key_placeholder_value(self):
        field_name = 'Api' + 'Key'
        content = '{"' + field_name + '": "CHANGE_ME"}'
        issues = fpc.check_api_keys('appsettings.json', content)
        self.assertEqual(issues, [])

    def _json(self, name, value):
        return '{"' + name + '": "' + value + '"}'

    def test_forbidden_patterns_detects_uppercase_and_token_password_names(self):
        for name in ('API' + '_KEY', 'CLIENT' + '_SECRET', 'Access' + 'Token', 'Db' + 'Password', 'Private' + '-Key'):
            with self.subTest(name=name):
                self.assertTrue(fpc.check_api_keys('a.json', self._json(name, 'abcd1234efgh')))

    def test_forbidden_patterns_detects_routing_service_keys(self):
        for name in ('Ors' + 'ApiKey', 'Routing' + 'ApiKey', 'OpenRouteService' + 'Key', 'Tankerkoenig' + 'ApiKey'):
            with self.subTest(name=name):
                self.assertTrue(fpc.check_api_keys('a.json', self._json(name, 'abcd1234efgh')))

    def test_forbidden_patterns_ignores_harmless_json_fields(self):
        for name, value in (('keyboard' + 'Type', 'numeric-keypad'), ('Primary' + 'Key', 'IdentityColumn'),
                            ('sort' + 'Key', 'name-ascending'), ('KeyVault' + 'Name', 'kv-development'),
                            ('PublicKey' + 'Token', 'b77a5c561934e089'), ('Key', 'Text')):
            with self.subTest(name=name):
                self.assertEqual(fpc.check_api_keys('a.json', self._json(name, value)), [])

    def test_forbidden_patterns_ignores_short_or_spaced_json_values(self):
        self.assertEqual(fpc.check_api_keys('a.json', self._json('Api' + 'Key', 'short')), [])
        self.assertEqual(fpc.check_api_keys('a.json', self._json('Api' + 'Key', 'bitte hier eintragen')), [])

    def test_forbidden_patterns_ignores_json_env_reference(self):
        self.assertEqual(fpc.check_api_keys('a.json', self._json('Api' + 'Key', '${TANKERKOENIG_KEY}')), [])

    def test_forbidden_patterns_detects_second_match_on_minified_line(self):
        first = '"Api' + 'Key": "' + 'CHANGE_ME_PLEASE' + '"'
        second = '"Client' + 'Secret": "' + 'realsecret123' + '"'
        content = '{' + first + ', ' + second + '}'
        issues = fpc.check_api_keys('a.json', content)
        self.assertEqual(len(issues), 1)


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

    def test_forbidden_patterns_detects_sql_dump(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'dump.sql').write_bytes(b'dummy')
            issues = fpc.check_file(root, 'dump.sql')
            self.assertTrue(any('Datenbank-Dump' in issue for issue in issues))

    def test_forbidden_patterns_detects_mobileprovision(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'embedded.mobileprovision').write_bytes(b'dummy')
            issues = fpc.check_file(root, 'embedded.mobileprovision')
            self.assertTrue(any('iOS-Signierungsdatei' in issue for issue in issues))

    def test_forbidden_patterns_detects_p8_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'AuthKey_ABC123.p8').write_bytes(b'dummy')
            issues = fpc.check_file(root, 'AuthKey_ABC123.p8')
            self.assertTrue(any('iOS-Signierungsdatei' in issue for issue in issues))

    def test_forbidden_patterns_blocks_other_log_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'app.log').write_bytes(b'x' * 100)
            issues = fpc.check_file(root, 'app.log')
            self.assertTrue(any('Logdatei nicht erlaubt' in issue for issue in issues))

    def test_forbidden_patterns_blocks_large_log_files_too(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            big_log = root / 'app.log'
            big_log.write_bytes(b'x' * (1024 * 1024 + 1))
            issues = fpc.check_file(root, 'app.log')
            self.assertTrue(any('Logdatei nicht erlaubt' in issue for issue in issues))

    def test_forbidden_patterns_allows_changes_log(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'changes.log').write_bytes(b'x' * (1024 * 1024 + 1))
            issues = fpc.check_file(root, 'changes.log')
            self.assertEqual(issues, [])

    def test_forbidden_patterns_blocks_changes_log_in_subfolder(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'docs').mkdir()
            (root / 'docs' / 'changes.log').write_bytes(b'x')
            issues = fpc.check_file(root, 'docs/changes.log')
            self.assertTrue(any('Logdatei nicht erlaubt' in issue for issue in issues))


if __name__ == '__main__':
    unittest.main()
