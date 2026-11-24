import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / "summarize-coverage.py"


class CoverageSummaryTests(unittest.TestCase):
    def run_summary(self, folder, *options):
        env = dict(os.environ)
        env.pop("GITHUB_STEP_SUMMARY", None)
        return subprocess.run([sys.executable, str(SCRIPT), str(folder), *options],
                              env=env, capture_output=True, text=True)

    def test_requires_a_real_report_and_valid_summary(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            self.assertNotEqual(self.run_summary(folder).returncode, 0)
            report = folder / "coverage.opencover.xml"
            for xml in ["<CoverageSession/>", '<CoverageSession><Summary sequenceCoverage="101"/></CoverageSession>',
                        '<CoverageSession><Summary sequenceCoverage="NaN"/></CoverageSession>']:
                report.write_text(xml, encoding="utf-8")
                self.assertNotEqual(self.run_summary(folder).returncode, 0)

    def test_reads_nested_opencover_reports_without_writing_repository_files(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory) / "test-run"
            folder.mkdir()
            (folder / "coverage.opencover.xml").write_text(
                '<CoverageSession><Summary sequenceCoverage="14.25"/></CoverageSession>', encoding="utf-8")
            result = self.run_summary(directory)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("14.25%", result.stdout)

    def test_focused_table_excludes_migrations_and_keeps_the_global_metric(self):
        xml = '''<CoverageSession><Summary sequenceCoverage="92.50"/>
            <Modules><Module><ModuleName>gestaotcc.WebApi</ModuleName><Classes>
                <Class><FullName>Gradus.Repositories</FullName>
                    <SequencePoint vc="1"/><BranchPoint vc="0"/>
                </Class>
                <Class><FullName>Gradus.Migrations.AddEntity</FullName>
                    <SequencePoint vc="0"/><BranchPoint vc="1"/>
                </Class>
            </Classes></Module></Modules></CoverageSession>'''
        with tempfile.TemporaryDirectory() as directory:
            (Path(directory) / "coverage.opencover.xml").write_text(xml, encoding="utf-8")
            result = self.run_summary(directory, "--sem-migrations", "--min-webapi", "85")
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("92.50%", result.stdout)
            self.assertIn("| gestaotcc.WebApi | 100.00% (1/1) | 0.00% (0/1) |", result.stdout)
            report = Path(directory) / "coverage.opencover.xml"
            report.write_text(xml.replace('SequencePoint vc="1"', 'SequencePoint vc="0"'), encoding="utf-8")
            failed = self.run_summary(directory, "--min-webapi", "85")
            self.assertNotEqual(failed.returncode, 0)
            self.assertIn("WebApi requires at least 85%", failed.stderr)


if __name__ == "__main__":
    unittest.main()
