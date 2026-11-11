import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / 'check-coverage.py'


class CoverageGateTests(unittest.TestCase):
    def run_gate(self, totals):
        with tempfile.TemporaryDirectory() as directory:
            for index, (visited, total) in enumerate(totals):
                report = Path(directory) / str(index) / 'coverage.opencover.xml'
                report.parent.mkdir()
                report.write_text(
                    f'<CoverageSession><Summary visitedSequencePoints="{visited}" '
                    f'numSequencePoints="{total}" /></CoverageSession>', encoding='utf-8')
            return subprocess.run([sys.executable, str(SCRIPT), directory, '90'],
                                  capture_output=True, text=True)

    def test_exact_threshold_passes(self):
        self.assertEqual(self.run_gate([(90, 100)]).returncode, 0)

    def test_below_threshold_never_rounds_up(self):
        self.assertNotEqual(self.run_gate([(89999, 100000)]).returncode, 0)

    def test_aggregation_uses_sequence_points(self):
        self.assertNotEqual(self.run_gate([(1, 1), (80, 100)]).returncode, 0)

    def test_missing_empty_and_invalid_coverage_fail(self):
        for totals in [[], [(0, 0)], [(101, 100)], [(-1, 100)]]:
            with self.subTest(totals=totals):
                self.assertNotEqual(self.run_gate(totals).returncode, 0)


if __name__ == '__main__':
    unittest.main()
