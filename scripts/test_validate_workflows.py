#!/usr/bin/env python3
"""Tests der .ipa-Prüfungen in validate-workflows.py (Verzeichnis-/Muster-Uploads und gh-release-Aufrufe)."""
import importlib.util
import sys
import unittest
from pathlib import Path

SPEC = importlib.util.spec_from_file_location('validate_workflows', Path(__file__).resolve().parent / 'validate-workflows.py')
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules['validate_workflows'] = MODULE
SPEC.loader.exec_module(MODULE)


def upload_errors(path, name='bericht'):
    errors = []
    MODULE.check_upload_artifact('wf', {'name': 'Upload', 'uses': 'actions/upload-artifact@v7', 'with': {'name': name, 'path': path}}, errors)
    return errors


def release_errors(run):
    errors = []
    MODULE.check_release_commands(run, 'wf', 'Release', errors)
    return errors


class UploadArtifactTests(unittest.TestCase):
    def test_known_safe_uploads_are_allowed(self):
        for path in ['coverage-report/', 'TestResults/*.trx', 'e2e-diagnostics/', 'vulnerable-packages.json', 'TestResults/**', 'artifacts/update.json']:
            self.assertEqual([], upload_errors(path), path)

    def test_explicit_ipa_is_rejected(self):
        self.assertTrue(upload_errors('release-ios.ipa'))
        self.assertTrue(upload_errors('coverage-report/', name='ios-ipa'))
        self.assertTrue(upload_errors('src/**/*.IPA'))

    def test_directories_that_could_contain_an_ipa_are_rejected(self):
        for path in ['artifacts/', 'artifacts', 'src/Tankradar.MAUI/bin/Release/', '.', './', '*', '**', 'artifacts/*', 'bin/**']:
            self.assertTrue(upload_errors(path), path)

    def test_patterns_and_unknown_extensions_are_rejected(self):
        for path in ['bin/*.app', 'publish/*', 'build/output.bin']:
            self.assertTrue(upload_errors(path), path)

    def test_multiline_paths_are_checked_individually(self):
        self.assertEqual([], upload_errors('coverage-report/\nTestResults/*.trx\n!TestResults/skip.trx'))
        self.assertEqual(1, len(upload_errors('coverage-report/\nartifacts/')))

    def test_unverifiable_and_escaping_paths_are_rejected(self):
        for path in ['${{ env.OUT }}', '$OUT/file.zip', '../outside.zip', '/abs/file.zip']:
            self.assertTrue(upload_errors(path), path)


class ReleaseCommandTests(unittest.TestCase):
    def test_current_release_commands_are_allowed(self):
        create = (
            'set -euo pipefail\n'
            'target_args=()\n'
            'gh release create "$RELEASE_TAG" artifacts/release-win-x64.zip artifacts/update.json \\\n'
            '  "${target_args[@]}" \\\n'
            '  --title "$RELEASE_TAG" \\\n'
            '  --generate-notes\n'
        )
        self.assertEqual([], release_errors(create))
        self.assertEqual([], release_errors('gh release upload "$RELEASE_TAG" artifacts/release-win-x64.zip artifacts/update.json --clobber'))
        self.assertEqual([], release_errors('gh release create "$RC_TAG" artifacts/release-win-x64.zip --target "$GITHUB_SHA" --title "$RC_TAG" --prerelease'))

    def test_ipa_asset_is_rejected(self):
        self.assertTrue(release_errors('gh release upload v1 release-ios.ipa'))
        self.assertTrue(release_errors('gh release create v1 artifacts/a.zip \\\n  build/App.ipa --title t'))

    def test_directory_pattern_and_variable_assets_are_rejected(self):
        for assets in ['artifacts/', 'artifacts/*', '"$ASSET"', '*.bin', 'src/Tankradar.MAUI/bin/Release/net10.0-ios']:
            self.assertTrue(release_errors(f'gh release upload v1 {assets}'), assets)

    def test_label_suffix_and_other_commands_are_handled(self):
        self.assertEqual([], release_errors('gh release upload v1 artifacts/update.json#Manifest'))
        self.assertEqual([], release_errors('gh release view v1\necho gh release'))
        self.assertEqual([], release_errors('gh release create v1 --notes "Hinweis: keine ipa"'))

    def test_workflows_in_repository_are_clean(self):
        errors = []
        for path in sorted((MODULE.ROOT / '.github' / 'workflows').glob('*.yml')):
            MODULE.check_workflow(path, errors)
        for path in sorted((MODULE.ROOT / '.github' / 'actions').glob('*/action.yml')):
            MODULE.check_action(path, errors)
        self.assertEqual([], errors)


if __name__ == '__main__':
    unittest.main()
