import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / 'check-coverage.py'


class CoverageGateTests(unittest.TestCase):
    def run_gate(self, totals, minimum='90'):
        with tempfile.TemporaryDirectory() as directory:
            for index, (visited, total) in enumerate(totals):
                report = Path(directory) / str(index) / 'coverage.opencover.xml'
                report.parent.mkdir()
                report.write_text(
                    f'<CoverageSession><Summary visitedSequencePoints="{visited}" '
                    f'numSequencePoints="{total}" /></CoverageSession>', encoding='utf-8')
            return subprocess.run([sys.executable, str(SCRIPT), directory, minimum],
                                  capture_output=True, text=True)

    def test_exact_threshold_passes(self):
        self.assertEqual(self.run_gate([(90, 100)]).returncode, 0)

    def test_required_95_percent_boundary(self):
        self.assertEqual(self.run_gate([(95, 100)], '95').returncode, 0)
        self.assertNotEqual(self.run_gate([(94999, 100000)], '95').returncode, 0)
        self.assertNotEqual(self.run_gate([(90, 100)], '95').returncode, 0)

    def test_below_threshold_never_rounds_up(self):
        self.assertNotEqual(self.run_gate([(89999, 100000)]).returncode, 0)

    def test_aggregation_uses_sequence_points(self):
        self.assertNotEqual(self.run_gate([(1, 1), (80, 100)]).returncode, 0)

    def test_missing_empty_and_invalid_coverage_fail(self):
        for totals in [[], [(0, 0)], [(101, 100)], [(-1, 100)]]:
            with self.subTest(totals=totals):
                self.assertNotEqual(self.run_gate(totals).returncode, 0)

    def test_badge_output_uses_approved_aggregate_without_rounding_up(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'github-output'
            for totals, expected in [([(1, 1), (959, 1000)], '95'),
                                     ([(100, 100)], '100'),
                                     ([(94999, 100000)], None)]:
                with self.subTest(totals=totals):
                    output.write_text('', encoding='utf-8')
                    # The helper starts a subprocess that inherits this environment.
                    with patch.dict(os.environ, {'GITHUB_OUTPUT': str(output)}):
                        result = self.run_gate(totals, '95')
                    if expected is None:
                        self.assertNotEqual(result.returncode, 0)
                        self.assertEqual(output.read_text(encoding='utf-8'), '')
                    else:
                        self.assertEqual(result.returncode, 0, result.stderr)
                        self.assertEqual(output.read_text(encoding='utf-8'),
                                         f'coverage_percentage={expected}\n')


if __name__ == '__main__':
    unittest.main()
