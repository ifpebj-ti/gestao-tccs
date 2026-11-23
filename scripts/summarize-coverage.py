"""Keep the existing OpenCover check without modifying README or uploading artifacts."""
import os
from pathlib import Path
import sys
import defusedxml.ElementTree as ET

reports = list(Path(sys.argv[1]).rglob("coverage.opencover.xml"))
if not reports:
    raise SystemExit("OpenCover coverage report missing")
lines = []


def coverage_without_migrations(module):
    points = {"SequencePoint": [0, 0], "BranchPoint": [0, 0]}
    for cls in module.findall("Classes/Class"):
        if ".Migrations." in (cls.findtext("FullName") or ""):
            continue
        for kind, totals in points.items():
            for point in cls.iter(kind):
                totals[0] += int(point.attrib["vc"]) > 0
                totals[1] += 1
    return points


def format_rate(points):
    visited, total = points
    return f"{100 * visited / total:.2f}% ({visited}/{total})" if total else "n/a"


for report in reports:
    root = ET.parse(report, forbid_dtd=True).getroot()
    summary = root.find("Summary")
    if summary is None:
        raise SystemExit(f"OpenCover Summary missing: {report}")
    coverage = float(summary.attrib["sequenceCoverage"])
    if not 0 <= coverage <= 100:
        raise SystemExit("Invalid coverage percentage")
    lines.append(f"Backend sequence coverage: {coverage:.2f}%")
    if "--sem-migrations" in sys.argv[2:]:
        lines.extend(["", "Cobertura por módulo sem migrations", "", "| Módulo | Sequências | Branches |", "| --- | --- | --- |"])
        total = {"SequencePoint": [0, 0], "BranchPoint": [0, 0]}
        for module in root.findall("Modules/Module"):
            points = coverage_without_migrations(module)
            if not points["SequencePoint"][1]:
                continue
            name = module.findtext("ModuleName")
            lines.append(f"| {name} | {format_rate(points['SequencePoint'])} | {format_rate(points['BranchPoint'])} |")
            for kind, (visited, count) in points.items():
                total[kind][0] += visited
                total[kind][1] += count
        lines.append(f"| **Total** | {format_rate(total['SequencePoint'])} | {format_rate(total['BranchPoint'])} |")
print("\n".join(lines))
if os.environ.get("GITHUB_STEP_SUMMARY"):
    with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as output:
        output.write("\n" + "\n".join(lines) + "\n")
