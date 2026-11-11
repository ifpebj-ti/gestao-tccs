"""Enforce aggregate OpenCover sequence coverage without rounding up."""
import os
import sys
from pathlib import Path
import defusedxml.ElementTree as ET

reports = list(Path(sys.argv[1]).rglob('coverage.opencover.xml'))
if not reports:
    sys.exit('Missing OpenCover coverage report')
visited = total = 0
for report in reports:
    summary = ET.parse(report, forbid_dtd=True).getroot().find('Summary')
    if summary is None:
        sys.exit(f'Missing coverage summary: {report}')
    visited += int(summary.attrib['visitedSequencePoints'])
    total += int(summary.attrib['numSequencePoints'])
if total <= 0 or visited < 0 or visited > total:
    sys.exit('Invalid coverage totals')
coverage = 100 * visited / total
message = f'Sequence coverage: {coverage:.2f}% ({visited}/{total}); minimum: {sys.argv[2]}%'
print(message)
if os.environ.get('GITHUB_STEP_SUMMARY'):
    with open(os.environ['GITHUB_STEP_SUMMARY'], 'a', encoding='utf-8') as output:
        output.write(message + '\n')
if coverage < float(sys.argv[2]):
    sys.exit(1)
