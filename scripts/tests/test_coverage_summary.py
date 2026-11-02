import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / "summarize-coverage.py"


class CoverageSummaryTests(unittest.TestCase):
    def run_summary(self, folder):
        env = dict(os.environ)
        env.pop("GITHUB_STEP_SUMMARY", None)
        return subprocess.run([sys.executable, str(SCRIPT), str(folder)],
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


if __name__ == "__main__":
    unittest.main()
