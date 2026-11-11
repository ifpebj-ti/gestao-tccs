import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class XmlReportSecurityTests(unittest.TestCase):
    def test_external_entities_and_dtd_are_rejected_without_reading_files(self):
        scripts = Path(__file__).resolve().parents[1]
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            secret = folder / 'private.txt'
            secret.write_text('PRIVATE-SYNTHETIC-CONTENT', encoding='utf-8')
            report = folder / 'coverage.opencover.xml'
            inputs = [
                '<!DOCTYPE CoverageSession><CoverageSession><Summary visitedSequencePoints="100" numSequencePoints="100" sequenceCoverage="100"/></CoverageSession>',
                f'<!DOCTYPE CoverageSession [<!ENTITY xxe SYSTEM "{secret.as_uri()}">]><CoverageSession>&xxe;</CoverageSession>',
            ]
            for xml in inputs:
                for name, arguments in [('check-coverage.py', ['95']), ('summarize-coverage.py', [])]:
                    with self.subTest(script=name, xml=xml):
                        report.write_text(xml, encoding='utf-8')
                        result = subprocess.run([sys.executable, str(scripts / name), directory, *arguments],
                                                capture_output=True, text=True)
                        self.assertNotEqual(result.returncode, 0)
                        self.assertIn('Forbidden', result.stderr)
                        self.assertNotIn('PRIVATE-SYNTHETIC-CONTENT', result.stdout + result.stderr)
