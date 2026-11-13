"""Keep the existing OpenCover check without modifying README or uploading artifacts."""
import os
from pathlib import Path
import sys
import defusedxml.ElementTree as ET

reports = list(Path(sys.argv[1]).rglob("coverage.opencover.xml"))
if not reports:
    raise SystemExit("OpenCover coverage report missing")
lines = []
for report in reports:
    summary = ET.parse(report, forbid_dtd=True).getroot().find("Summary")
    if summary is None:
        raise SystemExit(f"OpenCover Summary missing: {report}")
    coverage = float(summary.attrib["sequenceCoverage"])
    if not 0 <= coverage <= 100:
        raise SystemExit("Invalid coverage percentage")
    lines.append(f"Backend sequence coverage: {coverage:.2f}%")
print("\n".join(lines))
if os.environ.get("GITHUB_STEP_SUMMARY"):
    with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as output:
        output.write("\n" + "\n".join(lines) + "\n")
